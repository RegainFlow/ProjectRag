namespace ProjectRag.Contracts;

public sealed record StartIngestionRequest(
    string SourcePath);

public sealed record IngestionJobResponse(
    Guid IngestionId,
    string SourcePath,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt);
