namespace ProjectRag.Application.Models;

public sealed record Citation(
    Guid DocumentId,
    Guid ChunkId,
    string Source,
    double Score);