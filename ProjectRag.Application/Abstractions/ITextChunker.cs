using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}
