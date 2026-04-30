using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Tests.Support;

public sealed class FakeVectorIndexService : IVectorIndexService
{
    private readonly HashSet<Guid> _indexedDocumentIds = [];

    public List<VectorIndexChunk> UpsertedChunks { get; } = [];
    public List<Guid> DeletedDocumentIds { get; } = [];

    public Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        DeletedDocumentIds.Add(documentId);
        _indexedDocumentIds.Remove(documentId);

        return Task.CompletedTask;
    }

    public Task<bool> DocumentHasVectorsAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_indexedDocumentIds.Contains(documentId));
    }

    public Task UpsertChunksAsync(IReadOnlyCollection<VectorIndexChunk> chunks, CancellationToken cancellationToken)
    {
        UpsertedChunks.AddRange(chunks);

        foreach (var documentId in chunks.Select(x => x.DocumentId))
        {
            _indexedDocumentIds.Add(documentId);
        }

        return Task.CompletedTask;
    }
}