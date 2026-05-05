using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Tests.Support;

public sealed class FakeRerankerService : IRerankerService
{
    public Task<IReadOnlyList<SearchHit>> RerankAsync(RetrievalQuery query, IReadOnlyList<SearchHit> candidates, int topK, CancellationToken cancellationToken)
    {
        var results = candidates
            .Take(topK)
            .Select((hit, index) => hit with { RerankScore = 1d - (index * 0.1d) })
            .ToList();

        return Task.FromResult(results as IReadOnlyList<SearchHit>);
    }
}
