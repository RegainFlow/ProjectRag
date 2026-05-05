using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.Search;

internal sealed class HybridRetrievalSearchService : IRetrievalSearchService
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IKeywordSearchService _keywordSearchService;
    private readonly IRankFusionService _rankFusionService;

    public HybridRetrievalSearchService(
        IVectorSearchService vectorSearchService,
        IKeywordSearchService keywordSearchService,
        IRankFusionService rankFusionService)
    {
        _vectorSearchService = vectorSearchService;
        _keywordSearchService = keywordSearchService;
        _rankFusionService = rankFusionService;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(RetrievalQuery query, int topK, SearchFilters? filters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.OriginalQuery)
            && string.IsNullOrWhiteSpace(query.SemanticQuery)
            && string.IsNullOrWhiteSpace(query.KeywordQuery))
        {
            return [];
        }

        topK = Math.Clamp(topK, 1, 20);
        var candidateCount = Math.Max(topK * 3, 10);

        var vectorResultsTask = _vectorSearchService.SearchAsync(query.SemanticQuery, candidateCount, filters, cancellationToken);
        var keywordResultsTask = _keywordSearchService.SearchAsync(query.KeywordQuery, candidateCount, filters, cancellationToken);

        await Task.WhenAll(vectorResultsTask, keywordResultsTask);

        var vectorResults = await vectorResultsTask;
        var keywordResults = await keywordResultsTask;

        return await _rankFusionService.FuseAsync(
            vectorResults,
            keywordResults,
            topK,
            cancellationToken);
    }
}
