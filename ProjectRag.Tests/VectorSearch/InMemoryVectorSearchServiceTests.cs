using ProjectRag.Domain.Entities;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.VectorSearch;
using ProjectRag.Tests.Support;

namespace ProjectRag.Tests.VectorSearch;

public class InMemoryVectorSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_returns_most_similar_chunks_first()
    {
        using var database = new SqliteTestDatabase();
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

        var service = new InMemoryVectorSearchService(db, new FakeEmbeddingGenerator());

        var results = await service.SearchAsync("late payment fees", topK: 2, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal(latePaymentDocument.Id, results[0].DocumentId);
        Assert.Contains("monthly fee", results[0].Text);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public async Task SearchAsync_return_empty_results_for_blank_query()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();

        var service = new InMemoryVectorSearchService(db, new FakeEmbeddingGenerator());

        var results = await service.SearchAsync("", topK: 5, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_clamps_topK_to_valid_range()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();

        var document = new Document
        {
            Id = Guid.NewGuid(),
            SourceUri = "samples/docs/late-payment-policy.md",
            Title = "late-payment-policy",
            ContentHash = "hash-1",
            SourceType = "md",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        document.Chunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            ChunkIndex = 0,
            Text = "Invoices are due 30 calendar days after the invoice date.",
            Kind = ChunkKind.Paragraph,
            CreatedAt = DateTime.UtcNow
        });

        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var service = new InMemoryVectorSearchService(
            db,
            new FakeEmbeddingGenerator());

        var results = await service.SearchAsync(
            "invoice payment",
            topK: 0,
            CancellationToken.None);

        Assert.Single(results);
    }
}
