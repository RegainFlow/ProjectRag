namespace ProjectRag.Domain.Entities;

public sealed class Document
{
    public Guid Id { get; set; }
    public string SourceUri { get; set; } = "";
    public string Title { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string? SourceType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<DocumentChunk> Chunks { get; set; } = [];

}
