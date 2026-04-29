using ProjectRag.Domain.Enums;

namespace ProjectRag.Application.Models;

public sealed record ExtractedBlock(
    int BlockIndex,
    string Text,
    ChunkKind Kind,
    int? PageNumber,
    string? SectionTitle,
    string? LayoutRole,
    IReadOnlyList<ExtractedBoundingRegion> BoundingRegions);