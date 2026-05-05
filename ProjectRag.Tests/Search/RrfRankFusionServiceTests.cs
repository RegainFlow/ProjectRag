using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.Search;

namespace ProjectRag.Tests.Search;

public sealed class RrfRankFusionServiceTests
{

    private static SearchHit Hit(
        string text,
        double? vectorScore = null,
        double? keywordScore = null,
        Guid? chunkId = null)
    {
        return new SearchHit(
            Guid.NewGuid(),
            chunkId ?? Guid.NewGuid(),
            "source.md",
            text,
            RrfScore: 0,
            null,
            ChunkKind.Paragraph,
            null,
            VectorScore: vectorScore,
            KeywordScore: keywordScore);
    }

    [Fact]
    public async Task FuseAsync_scores_vector_only_result_by_rank()
    {
        var service = new RrfRankFusionService();

        var vectorHit = Hit("vector", vectorScore: 0.9);

        var results = await service.FuseAsync(
            [vectorHit],
            [],
            topK: 5,
            CancellationToken.None);

        var result = Assert.Single(results);

        Assert.Equal(vectorHit.ChunkId, result.ChunkId);
        Assert.Equal(1d / 61d, result.RrfScore);
        Assert.Equal(0.9, result.VectorScore);
        Assert.Null(result.KeywordScore);
        Assert.Equal("vector", result.MatchedBy);
    }

    [Fact]
    public async Task FuseAsync_scores_keyword_only_result_by_rank()
    {
        var service = new RrfRankFusionService();

        var keywordHit = Hit("keyword", keywordScore: 12);

        var results = await service.FuseAsync(
            [],
            [keywordHit],
            topK: 5,
            CancellationToken.None);

        var result = Assert.Single(results);

        Assert.Equal(keywordHit.ChunkId, result.ChunkId);
        Assert.Equal(1d / 61d, result.RrfScore);
        Assert.Null(result.VectorScore);
        Assert.Equal(12, result.KeywordScore);
        Assert.Equal("keyword", result.MatchedBy);
    }

    [Fact]
    public async Task FuseAsync_sums_scores_for_same_chunk_from_both_lists()
    {
        var service = new RrfRankFusionService();

        var chunkId = Guid.NewGuid();

        var vectorHit = Hit("vector", vectorScore: 0.9, chunkId: chunkId);
        var keywordHit = Hit("keyword", keywordScore: 12, chunkId: chunkId);

        var results = await service.FuseAsync(
            [vectorHit],
            [keywordHit],
            topK: 5,
            CancellationToken.None);

        var result = Assert.Single(results);

        Assert.Equal(chunkId, result.ChunkId);
        Assert.Equal((1d / 61d) + (1d / 61d), result.RrfScore);
        Assert.Equal(0.9, result.VectorScore);
        Assert.Equal(12, result.KeywordScore);
        Assert.Equal("hybrid", result.MatchedBy);
    }

    [Fact]
    public async Task FuseAsync_orders_by_rrf_score_descending()
    {
        var service = new RrfRankFusionService();

        var hybridChunkId = Guid.NewGuid();

        var vectorOnly = Hit("vector only", vectorScore: 0.99);
        var vectorHybrid = Hit("vector hybrid", vectorScore: 0.5, chunkId: hybridChunkId);
        var keywordHybrid = Hit("keyword hybrid", keywordScore: 3, chunkId: hybridChunkId);

        var results = await service.FuseAsync(
            [vectorOnly, vectorHybrid],
            [keywordHybrid],
            topK: 5,
            CancellationToken.None);

        Assert.Equal(hybridChunkId, results[0].ChunkId);
        Assert.True(results[0].RrfScore > results[1].RrfScore);
    }

    [Fact]
    public async Task FuseAsync_respects_topK()
    {
        var service = new RrfRankFusionService();

        var results = await service.FuseAsync(
            [
                Hit("one", vectorScore: 3),
                Hit("two", vectorScore: 2),
                Hit("three", vectorScore: 1)
            ],
            [],
            topK: 2,
            CancellationToken.None);

        Assert.Equal(2, results.Count);
    }
}
