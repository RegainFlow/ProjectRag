namespace ProjectRag.Application.Models;

public sealed record SearchHit(
    Guid DocumentId,
    Guid ChunkId,
    string Source,
    string Text,
    double Score);