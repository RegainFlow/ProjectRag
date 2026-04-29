using ProjectRag.Domain.Enums;

namespace ProjectRag.Application.Models;

public sealed record Citation(
    Guid DocumentId,
    Guid ChunkId,
    string Source,
    double Score,
    int? PageNumber,
    ChunkKind Kind,
    string? SectionTitle);