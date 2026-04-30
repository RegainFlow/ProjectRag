using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Infrastructure.Options;

namespace ProjectRag.Infrastructure.VectorSearch;

internal sealed class MevdVectorIndexService : IVectorIndexService
{
    private const int MaxDocumentVectorRecords = 10_000;

    private readonly VectorStoreCollection<string, DocumentChunkVectorRecord> _collection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly AiOptions _options;

    public MevdVectorIndexService(
        VectorStoreCollection<string, DocumentChunkVectorRecord> collection,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<AiOptions> options)
    {
        _collection = collection;
        _embeddingGenerator = embeddingGenerator;
        _options = options.Value;
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await _collection.EnsureCollectionExistsAsync(cancellationToken);

        var documentIdText = documentId.ToString();
        var keys = new List<string>();

        var records = _collection.GetAsync(
            x => x.DocumentId == documentIdText && x.EmbeddingModel == _options.EmbeddingModel,
            top: MaxDocumentVectorRecords,
            cancellationToken: cancellationToken);

        await foreach (var record in records)
        {
            keys.Add(record.ChunkId);
        }

        if (keys.Count > 0)
        {
            await _collection.DeleteAsync(keys, cancellationToken);
        }
    }

    public async Task<bool> DocumentHasVectorsAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await _collection.EnsureCollectionExistsAsync(cancellationToken);

        var documentIdText = documentId.ToString();

        var records = _collection.GetAsync(
            x => x.DocumentId == documentIdText && x.EmbeddingModel == _options.EmbeddingModel,
            top: 1,
            cancellationToken: cancellationToken);

        await foreach (var _ in records)
        {
            return true;
        }

        return false;
    }

    public async Task UpsertChunksAsync(IReadOnlyCollection<VectorIndexChunk> chunks, CancellationToken cancellationToken)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await _collection.EnsureCollectionExistsAsync(cancellationToken);

        var chunkList = chunks.ToList();
        var embeddings = await _embeddingGenerator.GenerateAsync(chunkList.Select(x => x.Text), cancellationToken: cancellationToken);

        var records = new List<DocumentChunkVectorRecord>(chunkList.Count);

        for (int i = 0; i < chunkList.Count; i++)
        {
            var chunk = chunkList[i];
            var embedding = embeddings[i];

            if (embedding.Dimensions != DocumentChunkVectorRecord.EmbeddingDimensions)
            {
                throw new InvalidOperationException($"Expected {DocumentChunkVectorRecord.EmbeddingDimensions} embedding dimensions, but got {embedding.Dimensions}.");
            }

            records.Add(new DocumentChunkVectorRecord
            {
                ChunkId = chunk.ChunkId.ToString(),
                DocumentId = chunk.DocumentId.ToString(),
                SourceUri = chunk.SourceUri,
                Text = chunk.Text,
                EmbeddingModel = _options.EmbeddingModel,
                Embedding = embedding.Vector
            });
        }

        await _collection.UpsertAsync(records, cancellationToken);
    }
}
