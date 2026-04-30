namespace ProjectRag.Application.Models;

public sealed record VectorIndexChunk(
    Guid DocumentId,
    Guid ChunkId,
    string SourceUri,
    string Text);
