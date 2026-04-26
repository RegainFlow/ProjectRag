using ProjectRag.Domain.Enums;

namespace ProjectRag.Domain.Entities;

public sealed class IngestionJob
{
    public Guid Id { get; set; }
    public string SourcePath { get; set; } = "";
    public IngestionJobStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
