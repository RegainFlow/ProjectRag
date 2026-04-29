namespace ProjectRag.Application.Models;

public sealed record ExtractedBoundingRegion(
    int PageNumber,
    IReadOnlyList<float> Polygon);