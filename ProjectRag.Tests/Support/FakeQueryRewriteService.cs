using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Tests.Support;

public sealed class FakeQueryRewriteService : IQueryRewriteService
{
    public Task<QueryRewrite> RewriteAsync(string query, CancellationToken cancellationToken)
    {
        var rewrite = new QueryRewrite(
            OriginalQuery: query,
            SemanticQuery: query,
            KeywordQuery: query,
            Status: "test-fake");

        return Task.FromResult(rewrite);
    }
}