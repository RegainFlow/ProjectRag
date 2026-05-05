using ProjectRag.Domain.Enums;

namespace ProjectRag.Application.Models;

public sealed record Citation(
    Guid DocumentId,
    Guid ChunkId,
    string Source,
    double RrfScore,
    double? VectorScore,
    double? KeywordScore,
    string MatchedBy,
    int? PageNumber,
    ChunkKind Kind,
    string? SectionTitle);