using Microsoft.EntityFrameworkCore;
using ProjectRag.Application.Abstractions;
using ProjectRag.Domain.Entities;
using ProjectRag.Domain.Enums;
using System.Security.Cryptography;

namespace ProjectRag.Infrastructure.Ingestion;

public sealed class FileSystemTextDocumentIngestionService : ITextDocumentIngestionService
{
    private static readonly string[] SupportedExtensions = [".md", ".txt"];

    private readonly RagDbContext _db;
    private readonly ITextChunker _chunker;

    public FileSystemTextDocumentIngestionService(
        RagDbContext db,
        ITextChunker chunker)
    {
        _db = db;
        _chunker = chunker;
    }

    public async Task IngestPathAsync(string sourcePath, CancellationToken cancellationToken)
    {
        var files = ResolveFiles(sourcePath);

        foreach (var file in files)
        {
            await IngestFileAsync(file, cancellationToken);
        }
    }

    private async Task IngestFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        var contentHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text)));

        var existing = await _db.Documents
            .Include(x => x.Chunks)
            .SingleOrDefaultAsync(x => x.SourceUri == filePath, cancellationToken);

        // skip document - content hasn't changed
        if (existing is not null && existing.ContentHash == contentHash)
        {
            return;
        }

        // ingest new document or reingest changed document
        if (existing is not null)
        {
            _db.DocumentChunks.RemoveRange(existing.Chunks);

            existing.Title = Path.GetFileNameWithoutExtension(filePath);
            existing.ContentHash = contentHash;
            existing.SourceType = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();
            existing.UpdatedAt = DateTime.UtcNow;

            AddChunks(existing, text);
        }
        else
        {
            var document = new Document
            {
                Id = Guid.NewGuid(),
                SourceUri = filePath,
                Title = Path.GetFileNameWithoutExtension(filePath),
                ContentHash = contentHash,
                SourceType = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            AddChunks(document, text);
            _db.Documents.Add(document);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private void AddChunks(Document document, string text)
    {
        foreach (var chunk in _chunker.Chunk(text))
        {
            document.Chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                ChunkIndex = chunk.ChunkIndex,
                Text = chunk.Text,
                SectionTitle = chunk.SectionTitle,
                Kind = ChunkKind.Paragraph,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static IReadOnlyList<string> ResolveFiles(string sourcePath)
    {
        if (File.Exists(sourcePath))
        {
            return IsSupportedFile(sourcePath) ? [Path.GetFullPath(sourcePath)] : [];
        }

        if (Directory.Exists(sourcePath))
        {
            return Directory
                .EnumerateFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(IsSupportedFile)
                .Select(Path.GetFullPath)
                .OrderBy(x => x)
                .ToList();
        }

        throw new FileNotFoundException($"Source path '{sourcePath}' was not found.", sourcePath);
    }

    private static bool IsSupportedFile(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
