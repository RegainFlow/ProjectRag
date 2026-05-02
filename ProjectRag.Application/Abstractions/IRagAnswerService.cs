using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IRagAnswerService
{
    Task<RagAnswer> AnswerAsync(string question, int topK, SearchFilters? filters, CancellationToken cancellationToken);
}
