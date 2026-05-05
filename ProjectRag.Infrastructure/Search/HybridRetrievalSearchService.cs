using Microsoft.Extensions.Options;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Infrastructure.Options;

namespace ProjectRag.Infrastructure.Search;

internal sealed class HybridRetrievalSearchService : IRetrievalSearchService
{
    private readonly RetrievalOptions _retrievalOptions;

    private readonly IVectorSearchService _vectorSearchService;
    private readonly IKeywordSearchService _keywordSearchService;
    private readonly IRankFusionService _rankFusionService;
    private readonly IRerankerService _rerankerService;

    public HybridRetrievalSearchService(
        IOptions<RetrievalOptions> retrievalOptions,
        IVectorSearchService vectorSearchService,
        IKeywordSearchService keywordSearchService,
        IRankFusionService rankFusionService,
        IRerankerService rerankerService
       )
    {
        _retrievalOptions = retrievalOptions.Value;
        _vectorSearchService = vectorSearchService;
        _keywordSearchService = keywordSearchService;
        _rankFusionService = rankFusionService;
        _rerankerService = rerankerService;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(RetrievalQuery query, int topK, SearchFilters? filters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.OriginalQuery)
            && string.IsNullOrWhiteSpace(query.SemanticQuery)
            && string.IsNullOrWhiteSpace(query.KeywordQuery))
        {
            return [];
        }

        topK = Math.Clamp(topK, 1, _retrievalOptions.MaxTopK);
        var candidateCount = Math.Max(topK, _retrievalOptions.CandidateCount);

        var vectorResultsTask = _vectorSearchService.SearchAsync(query.SemanticQuery, candidateCount, filters, cancellationToken);
        var keywordResultsTask = _keywordSearchService.SearchAsync(query.KeywordQuery, candidateCount, filters, cancellationToken);

        await Task.WhenAll(vectorResultsTask, keywordResultsTask);

        var vectorResults = await vectorResultsTask;
        var keywordResults = await keywordResultsTask;

        var fusedCandidates = await _rankFusionService.FuseAsync(
            vectorResults,
            keywordResults,
            candidateCount,
            cancellationToken);

        return await _rerankerService.RerankAsync(query, fusedCandidates, topK, cancellationToken);
    }
}
