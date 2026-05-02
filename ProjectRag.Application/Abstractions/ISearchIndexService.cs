using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface ISearchIndexService
{
    Task UpsertChunksAsync(IReadOnlyCollection<SearchIndexChunk> chunks, CancellationToken cancellationToken);
    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken);
    Task<bool> DocumentHasIndexedChunksAsync(Guid documentId, CancellationToken cancellationToken);
}
