using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Infrastructure.Options;
using System.Text.Json;

namespace ProjectRag.Infrastructure.AI;

internal sealed class RagAnswerService : IRagAnswerService
{
    private readonly AiOptions _aiOptions;

    private readonly IRetrievalSearchService _retrievalSearchService;
    private readonly IChatClient _chatClient;
    private readonly IQueryRewriteService _queryRewriteService;
    public RagAnswerService(
        IOptions<AiOptions> aiOptions,
        IRetrievalSearchService retrievalSearchService,
        IChatClient chatClient,
        IQueryRewriteService queryRewriteService)
    {
        _aiOptions = aiOptions.Value;
        _retrievalSearchService = retrievalSearchService;
        _chatClient = chatClient;
        _queryRewriteService = queryRewriteService;
    }
    public async Task<RagAnswer> AnswerAsync(
        string question,
        int topK,
        SearchFilters? filters,
        CancellationToken cancellationToken)
    {
        var queryRewrite = await _queryRewriteService.RewriteAsync(question, cancellationToken);

        if (string.IsNullOrWhiteSpace(question))
        {
            return new RagAnswer(
                Answer: "Please provide a question",
                AnswerStatus: "insufficientContext",
                QueryRewrite: queryRewrite,
                Claims: [],
                Citations: [],
                RetrievalDiagnostics: new RetrievalDiagnostics(
                    RequestedTopK: topK,
                    ReturnedContextCount: 0,
                    RerankingApplied: false),
                ModelInfo: BuildModelInfo());
        }

        var retrievalQuery = new RetrievalQuery(
            OriginalQuery: queryRewrite.OriginalQuery,
            SemanticQuery: queryRewrite.SemanticQuery,
            KeywordQuery: queryRewrite.KeywordQuery);

        var hits = await _retrievalSearchService.SearchAsync(
            retrievalQuery,
            topK,
            filters,
            cancellationToken);

        if (hits.Count == 0)
        {
            return new RagAnswer(
                Answer: "I do not have enough information in the available documents to answer that question.",
                AnswerStatus: "insufficientContext",
                QueryRewrite: queryRewrite,
                Claims: [],
                Citations: [],
                RetrievalDiagnostics: BuildRetrievalDiagnostics(topK, hits),
                ModelInfo: BuildModelInfo());
        }

        var context = BuildContext(hits);

        var prompt = $$"""
            You are a grounded RAG assistant.

            Use only the provided sources to answer the question.
            If the sources do not contain enough information, return insufficientContext.
            Do not use prior knowledge.
            Do not cite a source unless it directly supports the claim.

            Return JSON only. Do not include markdown fences, commentary, or explanations.

            The JSON object must have this shape:
            {
                "answerStatus": "answered",
                "answer": "concise answer text",
                "claims": [
                    {
                        "text": "specific claim from the answer",
                        "sourceIndexes": [1]
                    }
                ]
            }

            Rules:
            - answerStatus must be "answered" or "insufficientContext".
            - If answerStatus is "insufficientContext", answer must briefly say there is not enough information.
            - Each answered claim must include at least one source index.
            - Source indexes must refer to the [Source N] entries below.
            - Do not include source indexes that are not in the context.

            Context:
            {{context}}

            Question:
            {{question}}
            """;

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);

        var parsedAnswer = ParseAnswerResponse(response.Text, hits);

        var citations = hits
            .Select(hit => new Citation(
                hit.DocumentId,
                hit.ChunkId,
                hit.Source,
                hit.RrfScore,
                hit.RerankScore,
                hit.VectorScore,
                hit.KeywordScore,
                hit.MatchedBy,
                hit.PageNumber,
                hit.Kind,
                hit.SectionTitle
                ))
            .ToList();

        return new RagAnswer(
            Answer: parsedAnswer.Answer,
            AnswerStatus: parsedAnswer.AnswerStatus,
            QueryRewrite: queryRewrite,
            Claims: parsedAnswer.Claims,
            Citations: citations,
            RetrievalDiagnostics: BuildRetrievalDiagnostics(topK, hits),
            ModelInfo: BuildModelInfo());
    }

    private sealed record ParsedAnswer(
        string Answer,
        string AnswerStatus,
        IReadOnlyList<AnswerClaim> Claims);

    private static ParsedAnswer ParseAnswerResponse(string responseText, IReadOnlyList<SearchHit> hits)
    {
        try
        {
            using var document = JsonDocument.Parse(ExtractJsonObject(responseText));
            var root = document.RootElement;

            var answerStatus = root.TryGetProperty("answerStatus", out var statusElement) ? statusElement.GetString() : null;

            var answer = root.TryGetProperty("answer", out var answerElement) ? answerElement.GetString() : null;

            if (answerStatus is not ("answered" or "insufficientContext")
                || string.IsNullOrWhiteSpace(answer))
            {
                return InsufficientContext();
            }

            if (answerStatus == "insufficientContext")
            {
                return new ParsedAnswer(
                    Answer: answer,
                    AnswerStatus: "insufficientContext",
                    Claims: []);
            }

            var claims = ParseClaims(root, hits);

            if (claims.Count == 0)
            {
                return InsufficientContext();
            }

            return new ParsedAnswer(
                Answer: answer,
                AnswerStatus: "answered",
                Claims: claims);
        }
        catch (Exception)
        {
            return InsufficientContext();
        }
    }

    private static IReadOnlyList<AnswerClaim> ParseClaims(JsonElement root, IReadOnlyList<SearchHit> hits)
    {
        if (!root.TryGetProperty("claims", out var claimsElement)
            || claimsElement.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        var claims = new List<AnswerClaim>();

        foreach (var claimElement in claimsElement.EnumerateArray())
        {
            if (!claimElement.TryGetProperty("text", out var textElement)
                || !claimElement.TryGetProperty("sourceIndexes", out var sourceIndexesElement)
                || sourceIndexesElement.ValueKind is not JsonValueKind.Array)
            {
                continue;
            }

            var text = textElement.GetString();

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var citationChunkIds = sourceIndexesElement
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Number)
                .Select(x => x.GetInt32())
                .Where(index => index >= 1 && index <= hits.Count)
                .Select(index => hits[index - 1].ChunkId)
                .Distinct()
                .ToList();

            if (citationChunkIds.Count == 0)
            {
                continue;
            }

            claims.Add(new AnswerClaim(text, citationChunkIds));
        }

        return claims;
    }

    private static ParsedAnswer InsufficientContext()
    {
        return new ParsedAnswer(
            Answer: "I do not have enough information in the available documents to answer that question.",
            AnswerStatus: "insufficientContext",
            Claims: []);
    }

    private static string ExtractJsonObject(string responseText)
    {
        var start = responseText.IndexOf('{');
        var end = responseText.LastIndexOf('}');

        return start >= 0 && end > start
            ? responseText[start..(end + 1)]
            : responseText;
    }

    private static string BuildContext(IReadOnlyList<SearchHit> hits)
    {
        return string.Join(
            "\n\n",
            hits.Select((hit, index) => $"""
                [Source {index + 1}]
                DocumentId: {hit.DocumentId}
                ChunkId: {hit.ChunkId}
                Source: {hit.Source}
                RrfScore: {hit.RrfScore}
                RerankerScore: {hit.RerankScore}
                PageNumber: {hit.PageNumber}
                Kind: {hit.Kind}
                Section: {hit.SectionTitle}
                Text:
                {hit.Text}
                """));
    }

    private ModelInfo BuildModelInfo()
    {
        return new ModelInfo(
            ChatProvider: "Ollama",
            ChatModel: _aiOptions.ChatModel,
            EmbeddingModel: _aiOptions.EmbeddingModel);
    }

    private static RetrievalDiagnostics BuildRetrievalDiagnostics(
        int requestedTopK,
        IReadOnlyList<SearchHit> hits)
    {
        return new RetrievalDiagnostics(
            RequestedTopK: requestedTopK,
            ReturnedContextCount: hits.Count,
            RerankingApplied: hits.Any(hit => hit.RerankScore.HasValue));
    }
}
