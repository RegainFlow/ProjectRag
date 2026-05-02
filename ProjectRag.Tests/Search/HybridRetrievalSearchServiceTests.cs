using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.Search;

namespace ProjectRag.Tests.Search;

public sealed class HybridRetrievalSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_returns_empty_for_blank_query()
    {
        var service = new HybridRetrievalSearchService(
            new StubVectorSearchService([]),
            new StubKeywordSearchService([]));

        var results = await service.SearchAsync("", topK: 5, filters: null, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_includes_vector_only_hits()
    {
        var vectorHit = Hit("semantic late payment policy", score: 0.8);
        var service = new HybridRetrievalSearchService(
            new StubVectorSearchService([vectorHit]),
            new StubKeywordSearchService([]));

        var results = await service.SearchAsync(
            "late payment",
            topK: 5,
            filters: null,
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(vectorHit.ChunkId, result.ChunkId);
        Assert.Equal(1d, result.Score);
        Assert.Equal("vector", result.MatchedBy);
        Assert.NotNull(result.VectorScore);
        Assert.Null(result.KeywordScore);
    }

    [Fact]
    public async Task SearchAsync_includes_keyword_only_hits()
    {
        var keywordHit = Hit("exact keyword fee match", score: 12);
        var service = new HybridRetrievalSearchService(
            new StubVectorSearchService([]),
            new StubKeywordSearchService([keywordHit]));

        var results = await service.SearchAsync(
            "fee",
            topK: 5,
            filters: null,
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(keywordHit.ChunkId, result.ChunkId);
        Assert.Equal(1d, result.Score);
        Assert.Equal("keyword", result.MatchedBy);
        Assert.Null(result.VectorScore);
        Assert.NotNull(result.KeywordScore);
    }

    [Fact]
    public async Task SearchAsync_deduplicates_same_chunk_from_both_searches()
    {
        var chunkId = Guid.NewGuid();
        var vectorHit = Hit("vector match", score: 0.8, chunkId);
        var keywordHit = Hit("keyword match", score: 10, chunkId);
        var service = new HybridRetrievalSearchService(
            new StubVectorSearchService([vectorHit]),
            new StubKeywordSearchService([keywordHit]));

        var results = await service.SearchAsync(
            "late payment",
            topK: 5,
            filters: null,
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(chunkId, result.ChunkId);
    }

    [Fact]
    public async Task SearchAsync_boosts_chunk_matched_by_both_searches()
    {
        var hybridChunkId = Guid.NewGuid();
        var vectorOnly = Hit("vector only", score: 1);
        var keywordOnly = Hit("keyword only", score: 1);
        var vectorHybrid = Hit("hybrid vector", score: 1, hybridChunkId);
        var keywordHybrid = Hit("hybrid keyword", score: 1, hybridChunkId);

        var service = new HybridRetrievalSearchService(
            new StubVectorSearchService([vectorOnly, vectorHybrid]),
            new StubKeywordSearchService([keywordOnly, keywordHybrid]));

        var results = await service.SearchAsync(
            "late payment",
            topK: 5,
            filters: null,
            CancellationToken.None);

        Assert.Equal(hybridChunkId, results[0].ChunkId);
        Assert.True(results[0].Score > results[1].Score);
        Assert.Equal("hybrid", results[0].MatchedBy);
        Assert.NotNull(results[0].VectorScore);
        Assert.NotNull(results[0].KeywordScore);
    }

    [Fact]
    public async Task SearchAsync_respects_topK()
    {
        var service = new HybridRetrievalSearchService(
            new StubVectorSearchService(
            [
                Hit("vector one", score: 3),
                Hit("vector two", score: 2),
                Hit("vector three", score: 1)
            ]),
            new StubKeywordSearchService(
            [
                Hit("keyword one", score: 3),
                Hit("keyword two", score: 2),
                Hit("keyword three", score: 1)
            ]));

        var results = await service.SearchAsync(
            "late payment",
            topK: 2,
            filters: null,
            CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchAsync_passes_filters_to_vector_and_keyword_search()
    {
        var vectorSearch = new StubVectorSearchService([]);
        var keywordSearch = new StubKeywordSearchService([]);
        var service = new HybridRetrievalSearchService(vectorSearch, keywordSearch);

        var filters = new SearchFilters(SourceType: "md");

        await service.SearchAsync(
            "late payment",
            topK: 5,
            filters,
            CancellationToken.None);

        Assert.Same(filters, vectorSearch.ReceivedFilters);
        Assert.Same(filters, keywordSearch.ReceivedFilters);
    }

    private static SearchHit Hit(string text, double score, Guid? chunkId = null)
    {
        return new SearchHit(
            Guid.NewGuid(),
            chunkId ?? Guid.NewGuid(),
            "source.md",
            text,
            score,
            null,
            ChunkKind.Paragraph,
            null);
    }

    private sealed class StubVectorSearchService : IVectorSearchService
    {
        private readonly IReadOnlyList<SearchHit> _hits;
        public SearchFilters? ReceivedFilters { get; private set; }

        public StubVectorSearchService(IReadOnlyList<SearchHit> hits)
        {
            _hits = hits;
        }

        public Task<IReadOnlyList<SearchHit>> SearchAsync(
            string query,
            int topK,
            SearchFilters? filters,
            CancellationToken cancellationToken)
        {
            ReceivedFilters = filters;
            return Task.FromResult(_hits.Take(topK).ToList() as IReadOnlyList<SearchHit>);
        }
    }

    private sealed class StubKeywordSearchService : IKeywordSearchService
    {
        private readonly IReadOnlyList<SearchHit> _hits;
        public SearchFilters? ReceivedFilters { get; private set; }

        public StubKeywordSearchService(IReadOnlyList<SearchHit> hits)
        {
            _hits = hits;
        }

        public Task<IReadOnlyList<SearchHit>> SearchAsync(
            string query,
            int topK,
            SearchFilters? filters,
            CancellationToken cancellationToken)
        {
            ReceivedFilters = filters;
            return Task.FromResult(_hits.Take(topK).ToList() as IReadOnlyList<SearchHit>);
        }
    }
}
