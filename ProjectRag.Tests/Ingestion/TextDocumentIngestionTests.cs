using Microsoft.EntityFrameworkCore;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.Ingestion;
using ProjectRag.Tests.Support;

namespace ProjectRag.Tests.Ingestion;

public sealed class TextDocumentIngestionTests
{
    [Fact]
    public void Chunk_splits_text_into_ordered_chunks()
    {
        var chunker = new SimpleTextChunker();

        var text = """
            # Late Payment Policy

            Invoices are due 30 calendar days after the invoice date.

            Late balances may receive a monthly fee after a grace period.
            """;

        var chunks = chunker.Chunk(text);

        Assert.NotEmpty(chunks);
        Assert.Equal(0, chunks[0].ChunkIndex);
        Assert.Contains("Invoices are due", chunks[0].Text);
        Assert.Equal("Late Payment Policy", chunks[0].SectionTitle);
    }

    [Fact]
    public async Task IngestPathAsync_creates_document_and_chunks_for_markdown_file()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();

        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-ingestions-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "late-payment-policy.md");

            await File.WriteAllTextAsync(filePath, """
                # Late Payment Policy
            
                Invoices are due 30 calendar days after the invoice date.

                Late balances may receive a monthly fee after a grace period.
                """);

            var vectorIndexService = new FakeVectorIndexService();
            var service = new FileSystemDocumentIngestionService(
                db,
                new SimpleTextChunker(),
                new FakeDocumentExtractor(),
                vectorIndexService);

            await service.IngestPathAsync(filePath, CancellationToken.None);

            Assert.NotEmpty(vectorIndexService.UpsertedChunks);
            Assert.Contains(vectorIndexService.UpsertedChunks, x => x.Text.Contains("Invoices are due"));

            var document = await db.Documents
                .Include(x => x.Chunks)
                .SingleAsync();

            Assert.Equal(Path.GetFullPath(filePath), document.SourceUri);
            Assert.Equal("late-payment-policy", document.Title);
            Assert.Equal("md", document.SourceType);
            Assert.False(string.IsNullOrWhiteSpace(document.ContentHash));
            Assert.NotEmpty(document.Chunks);
            Assert.Contains(document.Chunks, x => x.Text.Contains("Invoices are due"));

        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IngestPathAsync_creates_layout_aware_chunks_for_scanned_document()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();

        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-scanned-ingestions-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "invoice.pdf");

            await File.WriteAllBytesAsync(filePath, [1, 2, 3, 4]);

            var vectorIndexService = new FakeVectorIndexService();
            var service = new FileSystemDocumentIngestionService(
                db,
                new SimpleTextChunker(),
                new FakeDocumentExtractor(),
                vectorIndexService);

            await service.IngestPathAsync(filePath, CancellationToken.None);

            Assert.Contains(vectorIndexService.UpsertedChunks, x => x.Text.Contains("Total amount due"));

            var document = await db.Documents
                .Include(x => x.Chunks)
                .SingleAsync();

            Assert.Equal(Path.GetFullPath(filePath), document.SourceUri);
            Assert.Equal("invoice", document.Title);
            Assert.Equal("pdf", document.SourceType);

            var heading = Assert.Single(document.Chunks, x => x.Kind == ChunkKind.Heading);

            Assert.Equal(1, heading.PageNumber);
            Assert.Equal("Invoice 1001", heading.SectionTitle);
            Assert.Equal("title", heading.LayoutRole);
            Assert.False(string.IsNullOrWhiteSpace(heading.BoundingRegionsJson));

            var paragraph = Assert.Single(document.Chunks, x => x.Kind == ChunkKind.Paragraph);

            Assert.Equal(1, paragraph.PageNumber);
            Assert.Equal("Invoice 1001", paragraph.SectionTitle);
            Assert.Contains("Total amount due", paragraph.Text);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IngestPathAsync_skips_unchanged_document_when_vectors_exist()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();

        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-idempotency-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "late-payment-policy.md");

            await File.WriteAllTextAsync(filePath, """
                # Late Payment Policy

                Invoices are due 30 calendar days after the invoice date.
                """);

            var vectorIndex = new FakeVectorIndexService();

            var service = new FileSystemDocumentIngestionService(
                db,
                new SimpleTextChunker(),
                new FakeDocumentExtractor(),
                vectorIndex);

            await service.IngestPathAsync(filePath, CancellationToken.None);
            await service.IngestPathAsync(filePath, CancellationToken.None);

            var documents = await db.Documents
                .Include(x => x.Chunks)
                .ToListAsync();

            var document = Assert.Single(documents);

            Assert.Single(document.Chunks);
            Assert.Single(vectorIndex.UpsertedChunks);
            Assert.Empty(vectorIndex.DeletedDocumentIds);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact(Skip = "Reingestion replacement behavior needs a separate EF tracking design pass.")]
    public async Task IngestPathAsync_replaces_chunks_and_vectors_when_file_changes()
    {
        using var database = new SqliteTestDatabase();
        await using var db = database.CreateContext();

        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-reingestion-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "late-payment-policy.md");

            await File.WriteAllTextAsync(filePath, """
                # Late Payment Policy

                Invoices are due 30 calendar days after the invoice date.
                """);

            var vectorIndex = new FakeVectorIndexService();

            var service = new FileSystemDocumentIngestionService(
                db,
                new SimpleTextChunker(),
                new FakeDocumentExtractor(),
                vectorIndex);

            await service.IngestPathAsync(filePath, CancellationToken.None);

            await File.WriteAllTextAsync(filePath, """
                # Late Payment Policy

                Late balances may receive a monthly fee after a grace period.
                """);

            await service.IngestPathAsync(filePath, CancellationToken.None);

            var document = await db.Documents
                .Include(x => x.Chunks)
                .SingleAsync();

            Assert.Single(document.Chunks);
            Assert.Contains(document.Chunks, x => x.Text.Contains("monthly fee"));
            Assert.Equal(2, vectorIndex.UpsertedChunks.Count);
            Assert.Single(vectorIndex.DeletedDocumentIds);
            Assert.Equal(document.Id, vectorIndex.DeletedDocumentIds[0]);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
