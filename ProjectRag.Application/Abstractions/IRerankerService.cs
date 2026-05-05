using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IRerankerService
{
    Task<IReadOnlyList<SearchHit>> RerankAsync(RetrievalQuery query, IReadOnlyList<SearchHit> candidates, int topK, CancellationToken cancellationToken);
}
