using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;

namespace ProjectRag.Infrastructure.DocumentIntelligence;

internal sealed record RawLayoutBlock(
    int SourceOffset,
    int SourceLength,
    string Text,
    ChunkKind Kind,
    int? PageNumber,
    string? SectionTitle,
    string? LayoutRole,
    IReadOnlyList<ExtractedBoundingRegion> BoundingRegions);
