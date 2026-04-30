using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.SqliteVec;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Entities;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.Options;
using ProjectRag.Infrastructure.VectorSearch;
using ProjectRag.Tests.Support;

namespace ProjectRag.Tests.VectorSearch;

public sealed class MevdVectorSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_returns_persisted_vector_results()
    {
        using var database = new SqliteFileTestDatabase();
        await using var db = database.CreateContext();

        var latePaymentDocument = new Document
        {
            Id = Guid.NewGuid(),
            SourceUri = "samples/docs/late-payment-policy.md",
            Title = "late-payment-policy",
            ContentHash = "hash-1",
            SourceType = "md",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        latePaymentDocument.Chunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            ChunkIndex = 0,
            Text = "Late balances may receive a monthly fee after a grace period.",
            Kind = ChunkKind.Paragraph,
            CreatedAt = DateTime.UtcNow
        });

        var securityDocument = new Document
        {
            Id = Guid.NewGuid(),
            SourceUri = "samples/docs/security-policy.md",
            Title = "security-policy",
            ContentHash = "hash-2",
            SourceType = "md",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        securityDocument.Chunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            ChunkIndex = 0,
            Text = "Multi-factor authentication is required for administrative tools.",
            Kind = ChunkKind.Paragraph,
            CreatedAt = DateTime.UtcNow
        });

        db.Documents.AddRange(latePaymentDocument, securityDocument);
        await db.SaveChangesAsync();

        using var collection = new SqliteCollection<string, DocumentChunkVectorRecord>(database.ConnectionString, "document_chunks");

        var options = Options.Create(new AiOptions
        {
            EmbeddingModel = "fake-embedding-model"
        });

        var indexService = new MevdVectorIndexService(collection, new FakeEmbeddingGenerator(), options);

        await indexService.UpsertChunksAsync(
            db.DocumentChunks.Select(chunk => new VectorIndexChunk(
            chunk.DocumentId,
            chunk.Id,
            chunk.Document!.SourceUri,
            chunk.Text)).ToList(),
        CancellationToken.None);

        var searchService = new MevdVectorSearchService(
            db,
            collection,
            new FakeEmbeddingGenerator(),
            options);

        var results = await searchService.SearchAsync("late payment fees", topK: 2, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(latePaymentDocument.Id, results[0].DocumentId);
        Assert.Contains("monthly fee", results[0].Text);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public async Task SearchAsync_return_empty_results_for_blank_query()
    {
        using var database = new SqliteFileTestDatabase();
        await using var db = database.CreateContext();

        using var collection =
            new SqliteCollection<string, DocumentChunkVectorRecord>(database.ConnectionString, "document_chunks");

        var service = new MevdVectorSearchService(
            db,
            collection,
            new FakeEmbeddingGenerator(),
            Options.Create(new AiOptions()));

        var results = await service.SearchAsync("", topK: 5, CancellationToken.None);

        Assert.Empty(results);
    }
}
