namespace ProjectRag.Application.Models;

public sealed record TextChunk(
    int ChunkIndex,
    string Text,
    string? SectionTitle = null);
