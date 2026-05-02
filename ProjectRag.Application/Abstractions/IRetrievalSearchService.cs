using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IRetrievalSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, SearchFilters? filters, CancellationToken cancellationToken);
}