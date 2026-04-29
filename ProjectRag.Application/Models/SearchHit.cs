using ProjectRag.Domain.Enums;

namespace ProjectRag.Application.Models;

public sealed record SearchHit(
    Guid DocumentId,
    Guid ChunkId,
    string Source,
    string Text,
    double Score,
    int? PageNumber,
    ChunkKind Kind,
    string? SectionTitle);