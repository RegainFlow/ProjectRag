using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Tests.Support;

public sealed class FakeVectorSearchService : IVectorSearchService
{
    public string? ReceivedQuery { get; private set; }
    public Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, SearchFilters? filters, CancellationToken cancellationToken)
    {
        ReceivedQuery = query;

        IReadOnlyList<SearchHit> results = [];
        return Task.FromResult(results);
    }
}