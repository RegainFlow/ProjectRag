using Microsoft.Extensions.AI;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.AI;

internal sealed class RagAnswerService : IRagAnswerService
{
    private readonly IVectorSearchService _vectorSearch;
    private readonly IChatClient _chatClient;

    public RagAnswerService(
        IVectorSearchService vectorSearch,
        IChatClient chatClient)
    {
        _vectorSearch = vectorSearch;
        _chatClient = chatClient;
    }

    public async Task<RagAnswer> AnswerAsync(
        string question,
        int topK,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new RagAnswer(
                "Please provide a question.",
                []);
        }

        var hits = await _vectorSearch.SearchAsync(question, topK, cancellationToken);

        if (hits.Count == 0)
        {
            return new RagAnswer(
                "I do not have enough information in the available dooucments to answer that question.",
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
                hit.Score,
                hit.PageNumber,
                hit.Kind,
                hit.SectionTitle
                ))
            .ToList();

        return new RagAnswer(response.Text, citations);
    }

    private static string BuildContext(IReadOnlyList<SearchHit> hits)
    {
        return string.Join(
            "\n\n",
            hits.Select((hit, index) => $"""
                [Source {index + 1}]
                DocumenId: {hit.DocumentId}
                ChunkId: {hit.ChunkId}
                Source: {hit.Source}
                Score: {hit.Score}
                PageNumber: {hit.PageNumber}
                Kind: {hit.Kind}
                Section: {hit.SectionTitle}
                Text:
                {hit.Text}

                """));
    }
}