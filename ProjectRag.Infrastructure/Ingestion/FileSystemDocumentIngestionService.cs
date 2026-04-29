using Microsoft.EntityFrameworkCore;
using ProjectRag.Application.Abstractions;
using ProjectRag.Domain.Entities;
using ProjectRag.Domain.Enums;
using System.Security.Cryptography;
using System.Text.Json;

namespace ProjectRag.Infrastructure.Ingestion;

internal sealed class FileSystemDocumentIngestionService : ITextDocumentIngestionService
{
    private static readonly string[] TextExtensions = [".md", ".txt"];
    private static readonly string[] ScannedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff"];
    private static readonly string[] SupportedExtensions = [.. TextExtensions, .. ScannedExtensions];

    private readonly RagDbContext _db;
    private readonly ITextChunker _chunker;
    private readonly IDocumentExtractor _documentExtractor;

    public FileSystemDocumentIngestionService(
        RagDbContext db,
        ITextChunker chunker,
        IDocumentExtractor documentExtractor)
    {
        _db = db;
        _chunker = chunker;
        _documentExtractor = documentExtractor;
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
        var contentHash = await ComputeFileHashAsync(filePath, cancellationToken);
        var extension = Path.GetExtension(filePath);

        var isTextDocument = TextExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

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

            if (isTextDocument)
            {
                var text = await File.ReadAllTextAsync(filePath, cancellationToken);
                AddTextChunks(existing, text);
            }
            else
            {
                await AddExtractedChunksAsync(existing, filePath, cancellationToken);
            }
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


            if (isTextDocument)
            {
                var text = await File.ReadAllTextAsync(filePath, cancellationToken);
                AddTextChunks(document, text);
            }
            else
            {
                await AddExtractedChunksAsync(document, filePath, cancellationToken);
            }

            _db.Documents.Add(document);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task AddExtractedChunksAsync(Document document, string filePath, CancellationToken cancellationToken)
    {
        var extracted = await _documentExtractor.ExtractAsync(filePath, cancellationToken);
        var currentSectionTitle = default(string);

        foreach (var block in extracted.Blocks)
        {
            if (!string.IsNullOrWhiteSpace(block.SectionTitle))
            {
                currentSectionTitle = block.SectionTitle;
            }

            document.Chunks.Add(new DocumentChunk
            {
                Id = Guid.NewGuid(),
                ChunkIndex = block.BlockIndex,
                Text = block.Text,
                PageNumber = block.PageNumber,
                SectionTitle = block.SectionTitle ?? currentSectionTitle,
                LayoutRole = block.LayoutRole,
                BoundingRegionsJson = JsonSerializer.Serialize(block.BoundingRegions),
                Kind = block.Kind,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private void AddTextChunks(Document document, string text)
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

    private static async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);

        return Convert.ToHexString(hash);
    }

    private static bool IsSupportedFile(string path)
    {
        var extension = Path.GetExtension(path);
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }
}
