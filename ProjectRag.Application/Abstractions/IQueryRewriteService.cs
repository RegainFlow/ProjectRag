using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IQueryRewriteService
{
    Task<QueryRewrite> RewriteAsync(string query, CancellationToken cancellationToken);
}