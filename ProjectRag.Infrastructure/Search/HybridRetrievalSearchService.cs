using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.Search;

internal sealed class HybridRetrievalSearchService : IRetrievalSearchService
{
    private const double HybridMatchBonus = 0.25;

    private readonly IVectorSearchService _vectorSearchService;
    private readonly IKeywordSearchService _keywordSearchService;

    public HybridRetrievalSearchService(
        IVectorSearchService vectorSearchService,
        IKeywordSearchService keywordSearchService)
    {
        _vectorSearchService = vectorSearchService;
        _keywordSearchService = keywordSearchService;
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

        var vectorScores = NormalizeScores(vectorResults);
        var keywordScores = NormalizeScores(keywordResults);

        var hitsByChunkId = vectorResults
            .Concat(keywordResults)
            .GroupBy(x => x.ChunkId)
            .ToDictionary(
                x => x.Key,
                x => x.First());

        return hitsByChunkId
            .Select(pair =>
            {
                var chunkId = pair.Key;
                var hit = pair.Value;

                var hasVectorScore = vectorScores.TryGetValue(chunkId, out var vectorScore);
                var hasKeywordScore = keywordScores.TryGetValue(chunkId, out var keywordScore);

                var mergedScore = vectorScore + keywordScore;

                if (hasVectorScore && hasKeywordScore)
                {
                    mergedScore += HybridMatchBonus;
                }

                var matchedBy = hasVectorScore && hasKeywordScore ? "hybrid" : hasVectorScore ? "vector" : "keyword";

                return hit with
                {
                    Score = mergedScore,
                    VectorScore = hasVectorScore ? vectorScore : null,
                    KeywordScore = hasKeywordScore ? keywordScore : null,
                    MatchedBy = matchedBy
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    private static Dictionary<Guid, double> NormalizeScores(IReadOnlyList<SearchHit> hits)
    {
        if (hits.Count == 0)
        {
            return [];
        }

        var maxScore = hits.Max(x => x.Score);

        if (maxScore <= 0)
        {
            return hits.ToDictionary(x => x.ChunkId, _ => 0d);
        }

        return hits.ToDictionary(
            x => x.ChunkId,
            x => x.Score / maxScore);
    }
}
