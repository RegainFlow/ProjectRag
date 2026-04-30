using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Infrastructure.Options;

namespace ProjectRag.Infrastructure.VectorSearch;

internal sealed class MevdVectorSearchService : IVectorSearchService
{
    private readonly RagDbContext _db;
    private readonly VectorStoreCollection<string, DocumentChunkVectorRecord> _collection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly AiOptions _options;

    public MevdVectorSearchService(
        RagDbContext db,
        VectorStoreCollection<string, DocumentChunkVectorRecord> collections,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IOptions<AiOptions> options)
    {
        _db = db;
        _collection = collections;
        _embeddingGenerator = embeddingGenerator;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        topK = Math.Clamp(topK, 1, 20);

        await _collection.EnsureCollectionExistsAsync(cancellationToken);

        var queryEmbedding = await _embeddingGenerator.GenerateVectorAsync(query, cancellationToken: cancellationToken);

        var vectorResults = new List<VectorSearchResult<DocumentChunkVectorRecord>>();

        var results = _collection.SearchAsync(
            queryEmbedding,
            topK,
            new VectorSearchOptions<DocumentChunkVectorRecord>
            {
                Filter = record => record.EmbeddingModel == _options.EmbeddingModel
            },
            cancellationToken);

        // store queried vector results
        await foreach (var result in results)
        {
            vectorResults.Add(result);
        }

        if (vectorResults.Count == 0)
        {
            return [];
        }

        var chunkIds = vectorResults.Select(x => Guid.Parse(x.Record.ChunkId)).ToList();

        // get document chunks from db
        var chunks = await _db.DocumentChunks
            .AsNoTracking()
            .Include(x => x.Document)
            .Where(x => chunkIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var chunksById = chunks.ToDictionary(x => x.Id);

        // get matched chunk from queried result
        return vectorResults
            .Select(result =>
            {
                var chunkId = Guid.Parse(result.Record.ChunkId);
                var chunk = chunksById[chunkId];

                // SQLiteVec returns cosine distance because our vector property uses CosineDistance.
                // Convert to a similarity-style score so higher is better for API callers.
                var score = result.Score is null ? 0 : 1 - result.Score.Value;

                return new SearchHit(
                    chunk.DocumentId,
                    chunk.Id,
                    chunk.Document?.SourceUri ?? result.Record.SourceUri,
                    chunk.Text,
                    score,
                    chunk.PageNumber,
                    chunk.Kind,
                    chunk.SectionTitle);
            })
            .ToList();
    }
}