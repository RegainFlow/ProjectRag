using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.Ingestion;

public sealed class SimpleTextChunker : ITextChunker
{
    private const int MaxChunkSize = 1_200;

    public IReadOnlyList<TextChunk> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var paragraphs = text
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var chunks = new List<TextChunk>();
        var current = new List<string>();
        var currentLength = 0;

        foreach (var paragraph in paragraphs)
        {
            if (currentLength + paragraph.Length > MaxChunkSize && current.Count > 0)
            {
                AddChunk(chunks, current);
                current.Clear();
                currentLength = 0;
            }

            current.Add(paragraph);
            currentLength += paragraph.Length;
        }

        if (current.Count > 0)
        {
            AddChunk(chunks, current);
        }

        return chunks;
    }

    private static void AddChunk(List<TextChunk> chunks, List<string> paragraphs)
    {
        var text = string.Join("\n\n", paragraphs).Trim();

        chunks.Add(new TextChunk(
            chunks.Count,
            text,
            ExtractSectionTitle(text)));
    }

    private static string? ExtractSectionTitle(string text)
    {
        var firstLine = text
            .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        return firstLine is not null && firstLine.StartsWith("# ", StringComparison.Ordinal)
            ? firstLine[2..].Trim()
            : null;
    }
}
