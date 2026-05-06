using Microsoft.Extensions.Options;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.AI;
using ProjectRag.Infrastructure.Options;
using ProjectRag.Tests.Support;

namespace ProjectRag.Tests.AI;

public sealed class LlmRerankerServiceTests
{
    [Fact]
    public async Task RerankAsync_orders_candidates_by_llm_score()
    {
        var chatClient = new FakeChatClient("""
              {
                "scores": [
                  { "index": 1, "score": 0.10 },
                  { "index": 2, "score": 0.95 }
                ]
              }
              """);

        var service = new LlmRerankerService(
            chatClient,
            Options.Create(new RetrievalOptions()));

        var first = Hit("less relevant");
        var second = Hit("late payment fee details");

        var results = await service.RerankAsync(
            Query("late payment fees"),
            [first, second],
            topK: 2,
            CancellationToken.None);

        Assert.Equal(second.ChunkId, results[0].ChunkId);
        Assert.Equal(0.95, results[0].RerankScore);

        Assert.Equal(first.ChunkId, results[1].ChunkId);
        Assert.Equal(0.10, results[1].RerankScore);
    }

    [Fact]
    public async Task RerankAsync_falls_back_to_rrf_order_when_json_is_invalid()
    {
        var chatClient = new FakeChatClient("not json");

        var service = new LlmRerankerService(
            chatClient,
            Options.Create(new RetrievalOptions()));

        var first = Hit("first");
        var second = Hit("second");

        var results = await service.RerankAsync(
            Query("anything"),
            [first, second],
            topK: 2,
            CancellationToken.None);

        Assert.Equal(first.ChunkId, results[0].ChunkId);
        Assert.Equal(second.ChunkId, results[1].ChunkId);

        Assert.All(results, result => Assert.Null(result.RerankScore));
    }

    [Fact]
    public async Task RerankAsync_assigns_zero_to_candidates_missing_from_response()
    {
        var chatClient = new FakeChatClient("""
          {
            "scores": [
              { "index": 2, "score": 0.80 }
            ]
          }
          """);

        var service = new LlmRerankerService(
            chatClient,
            Options.Create(new RetrievalOptions()));

        var first = Hit("unscored candidate");
        var second = Hit("scored candidate");

        var results = await service.RerankAsync(
            Query("anything"),
            [first, second],
            topK: 2,
            CancellationToken.None);

        Assert.Equal(second.ChunkId, results[0].ChunkId);
        Assert.Equal(0.80, results[0].RerankScore);

        Assert.Equal(first.ChunkId, results[1].ChunkId);
        Assert.Equal(0, results[1].RerankScore);
    }

    private static RetrievalQuery Query(string query)
    {
        return new RetrievalQuery(
            OriginalQuery: query,
            SemanticQuery: query,
            KeywordQuery: query);
    }

    private static SearchHit Hit(string text)
    {
        return new SearchHit(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "source.md",
            text,
            RrfScore: 0.01,
            PageNumber: null,
            ChunkKind.Paragraph,
            SectionTitle: null,
            MatchedBy: "hybrid");
    }
}
