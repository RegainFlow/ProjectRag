using ProjectRag.Domain.Enums;

namespace ProjectRag.Application.Models;

public sealed record SearchHit(
    Guid DocumentId,
    Guid ChunkId,
    string Source,
    string Text,
    double RrfScore,
    int? PageNumber,
    ChunkKind Kind,
    string? SectionTitle,
    double? VectorScore = null,
    double? KeywordScore = null,
    string MatchedBy = "unknown");