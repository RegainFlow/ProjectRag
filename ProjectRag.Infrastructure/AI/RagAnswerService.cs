using Microsoft.Extensions.AI;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.AI;

internal sealed class RagAnswerService : IRagAnswerService
{
    private readonly IRetrievalSearchService _retrievalSearchService;
    private readonly IChatClient _chatClient;
    private readonly IQueryRewriteService _queryRewriteService;
    public RagAnswerService(
        IRetrievalSearchService retrievalSearchService,
        IChatClient chatClient,
        IQueryRewriteService queryRewriteService)
    {
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
                "Please provide a question.",
                queryRewrite,
                []);
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
                "I do not have enough information in the available documents to answer that question.",
                queryRewrite,
                []);
        }

        var context = BuildContext(hits);

        var prompt = $"""
            You are a grounded RAG assistant.

            Answer the user's question using only the context below.
            If the context does not contain the answer, say you do not have enough information.
            Keep the answer concise.

            Context"
            {context}

            Question:
            {question}
            """;

        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);

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

        return new RagAnswer(response.Text, queryRewrite, citations);
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
                Score: {hit.RrfScore}
                PageNumber: {hit.PageNumber}
                Kind: {hit.Kind}
                Section: {hit.SectionTitle}
                Text:
                {hit.Text}
                """));
    }
}
