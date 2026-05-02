using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IKeywordSearchService
{
    Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, SearchFilters? filters, CancellationToken cancellationToken);
}
