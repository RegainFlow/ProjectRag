using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.Search;

internal sealed class RrfRankFusionService : IRankFusionService
{
    private const int RrfConstant = 60;
    public Task<IReadOnlyList<SearchHit>> FuseAsync(IReadOnlyList<SearchHit> vectorResults, IReadOnlyList<SearchHit> keywordResults, int topK, CancellationToken cancellationToken)
    {
        topK = Math.Clamp(topK, 1, 20);

        var candidates = new Dictionary<Guid, FusionCandidate>();

        AddResults(candidates, vectorResults, RetrievalSource.Vector);
        AddResults(candidates, keywordResults, RetrievalSource.Keyword);

        var results = candidates.Values
            .Select(candidate =>
            {
                var matchedBy = candidate.HasVectorScore && candidate.HasKeywordScore ? "hybrid" : candidate.HasVectorScore ? "vector" : "keyword";

                return candidate.RepresentativeHit with
                {
                    RrfScore = candidate.RrfScore,
                    VectorScore = candidate.VectorScore,
                    KeywordScore = candidate.KeywordScore,
                    MatchedBy = matchedBy,
                };
            })
            .OrderByDescending(x => x.RrfScore)
            .Take(topK)
            .ToList();

        return Task.FromResult(results as IReadOnlyList<SearchHit>);
    }

    private static void AddResults(Dictionary<Guid, FusionCandidate> candidates, IReadOnlyList<SearchHit> hits, RetrievalSource source)
    {
        for (int index = 0; index < hits.Count; index++)
        {
            var hit = hits[index];
            var rank = index + 1;
            var rrfContribution = 1d / (RrfConstant + rank);

            if (!candidates.TryGetValue(hit.ChunkId, out var candidate))
            {
                candidate = new FusionCandidate(hit);
                candidates[hit.ChunkId] = candidate;
            }

            candidate.RrfScore += rrfContribution;

            if (source == RetrievalSource.Vector)
            {
                candidate.VectorScore = hit.VectorScore;
                candidate.HasVectorScore = true;
            }
            else
            {
                candidate.KeywordScore = hit.KeywordScore;
                candidate.HasKeywordScore = true;
            }
        }
    }

    private sealed class FusionCandidate
    {
        public SearchHit RepresentativeHit { get; }
        public FusionCandidate(SearchHit representativeHit)
        {
            RepresentativeHit = representativeHit;
        }

        public double RrfScore { get; set; }
        public double? VectorScore { get; set; }
        public double? KeywordScore { get; set; }
        public bool HasVectorScore { get; set; }
        public bool HasKeywordScore { get; set; }
    }

    private enum RetrievalSource
    {
        Vector,
        Keyword,
    }
}

