using ProjectRag.Domain.Enums;

namespace ProjectRag.Domain.Entities;

public sealed class IngestionJob
{
    public Guid Id { get; set; }
    public string SourcePath { get; set; } = "";
    public IngestionJobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
