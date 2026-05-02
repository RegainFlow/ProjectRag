using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Tests.Support;

public sealed class FakeVectorSearchService : IVectorSearchService
{
    public SearchFilters? ReceivedFilters { get; private set; }
    public Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, SearchFilters? filters, CancellationToken cancellationToken)
    {
        ReceivedFilters = filters;

        IReadOnlyList<SearchHit> results = [];
        return Task.FromResult(results);
    }
}