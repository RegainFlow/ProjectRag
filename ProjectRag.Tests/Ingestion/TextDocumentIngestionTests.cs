using Microsoft.EntityFrameworkCore;
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

            var service = new FileSystemTextDocumentIngestionService(
                db,
                new SimpleTextChunker());

            await service.IngestPathAsync(filePath, CancellationToken.None);

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
}
