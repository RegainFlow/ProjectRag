using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using System.Numerics.Tensors;

namespace ProjectRag.Infrastructure.VectorSearch;

public class InMemoryVectorSearchService : IVectorSearchService
{
    private readonly RagDbContext _db;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public InMemoryVectorSearchService(
        RagDbContext db,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _db = db;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        topK = Math.Clamp(topK, 1, 20);

        var queryEmbedding = await _embeddingGenerator.GenerateVectorAsync(query, cancellationToken: cancellationToken);

        var chunks = await _db.DocumentChunks
            .AsNoTracking()
            .Include(x => x.Document)
            .ToListAsync(cancellationToken);

        var results = new List<SearchHit>();

        foreach (var chunk in chunks)
        {
            var chunkEmbedding = await _embeddingGenerator.GenerateVectorAsync(chunk.Text, cancellationToken: cancellationToken);

            var score = TensorPrimitives.CosineSimilarity(queryEmbedding.Span, chunkEmbedding.Span);

            results.Add(new SearchHit(
                chunk.DocumentId,
                chunk.Id,
                chunk.Document?.SourceUri ?? "",
                chunk.Text,
                score));
        }

        return results
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }
}
