using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IVectorIndexService
{
    Task UpsertChunksAsync(IReadOnlyCollection<VectorIndexChunk> chunks, CancellationToken cancellationToken);

    Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<bool> DocumentHasVectorsAsync(Guid documentId, CancellationToken cancellationToken);
}