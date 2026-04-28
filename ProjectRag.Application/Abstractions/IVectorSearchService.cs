using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IVectorSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, CancellationToken cancellationToken);
}