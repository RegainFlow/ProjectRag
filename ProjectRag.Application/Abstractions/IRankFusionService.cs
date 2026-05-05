using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IRankFusionService
{
    Task<IReadOnlyList<SearchHit>> FuseAsync(IReadOnlyList<SearchHit> vectorResults, IReadOnlyList<SearchHit> keywordResults, int topK, CancellationToken cancellationToken);
}
