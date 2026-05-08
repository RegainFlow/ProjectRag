using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Infrastructure.Options;
using ProjectRag.Application.Telemetry;
using System.Text;
using System.Text.Json;

namespace ProjectRag.Infrastructure.AI;

internal sealed class LlmRerankerService : IRerankerService
{
    private readonly IChatClient _chatClient;
    private readonly RetrievalOptions _retrievalOptions;

    public LlmRerankerService(
        IChatClient chatClient,
        IOptions<RetrievalOptions> retrievalOptions)
    {
        _chatClient = chatClient;
        _retrievalOptions = retrievalOptions.Value;
    }

    public async Task<IReadOnlyList<SearchHit>> RerankAsync(RetrievalQuery query, IReadOnlyList<SearchHit> candidates, int topK, CancellationToken cancellationToken)
    {
        using var activity = ProjectRagTelemetry.ActivitySource.StartActivity("rag.rerank");
        activity?.SetTag("rag.query.length", query.OriginalQuery.Length);
        activity?.SetTag("rag.candidates.count", candidates.Count);
        activity?.SetTag("rag.top_k", topK);

        if (candidates.Count == 0)
        {
            activity?.SetTag("rag.results.count", 0);
            return [];
        }

        topK = Math.Clamp(topK, 1, _retrievalOptions.MaxTopK);
        activity?.SetTag("rag.top_k.effective", topK);

        try
        {
            var response = await _chatClient.GetResponseAsync(BuildPrompt(query, candidates), cancellationToken: cancellationToken);

            var results = ApplyScores(candidates, response.Text, topK);

            activity?.SetTag("rag.results.count", results.Count);
            activity?.SetTag("rag.rerank.fallback", false);

            return results;
        }
        catch (Exception ex)
        {
            var fallbackResults = candidates.Take(topK).ToList();

            activity?.SetTag("rag.results.count", fallbackResults.Count);
            activity?.SetTag("rag.rerank.fallback", true);
            activity?.SetTag("rag.error.type", ex.GetType().Name);

            return fallbackResults;
        }
    }

    private static IReadOnlyList<SearchHit> ApplyScores(IReadOnlyList<SearchHit> candidates, string responseText, int topK)
    {
        using var document = JsonDocument.Parse(responseText);

        if (!document.RootElement.TryGetProperty("scores", out var scoresElement)
            || scoresElement.ValueKind is not JsonValueKind.Array)
        {
            return candidates.Take(topK).ToList();
        }

        var scoresByIndex = new Dictionary<int, double>();

        foreach (var scoreElement in scoresElement.EnumerateArray())
        {
            if (!scoreElement.TryGetProperty("index", out var indexElement)
                || !scoreElement.TryGetProperty("score", out var scoreValueElement))
            {
                continue;
            }

            var index = indexElement.GetInt32();
            var score = scoreValueElement.GetDouble();

            if (index < 1 || index > candidates.Count)
            {
                continue;
            }

            scoresByIndex[index] = Math.Clamp(score, 0, 1);
        }

        if (scoresByIndex.Count == 0)
        {
            return candidates.Take(topK).ToList();
        }

        return candidates
            .Select((candidate, index) =>
            {
                var oneBasedIndex = index + 1;
                var rerankScore = scoresByIndex.TryGetValue(oneBasedIndex, out var score) ? score : 0;

                return candidate with { RerankScore = rerankScore };
            })
            .OrderByDescending(x => x.RerankScore)
            .ThenByDescending(x => x.RrfScore)
            .Take(topK)
            .ToList();
    }

    private string BuildPrompt(RetrievalQuery query, IReadOnlyList<SearchHit> candidates)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You are a search result reranker for a retrieval-augmented generation system.");
        builder.AppendLine();
        builder.AppendLine("Score each candidate from 0.0 to 1.0 based only on relevance to the user question");
        builder.AppendLine("Return JSON only. Do not include markdown fences or commentary.");
        builder.AppendLine();
        builder.AppendLine("The JSON object must have this shape:");
        builder.AppendLine("""{"scores":[{"index":1,"score":0.95},{"index":2,"score":0.12}]}""");
        builder.AppendLine();
        builder.AppendLine("User question:");
        builder.AppendLine(query.OriginalQuery);
        builder.AppendLine();
        builder.AppendLine("Candidates:");

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            builder.AppendLine($"Index: {i + 1}");
            builder.AppendLine($"Section: {candidate.SectionTitle}");
            builder.AppendLine($"Kind: {candidate.Kind}");
            builder.AppendLine($"Text: {Truncate(candidate.Text, _retrievalOptions.RerankerMaxTextChars)}");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text[..maxLength];
    }
}
