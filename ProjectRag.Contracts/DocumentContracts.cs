namespace ProjectRag.Contracts;

public sealed record DocumentSummaryResponse(
    string DocumentId,
    string SourceUri,
    string Title,
    DateTime CreatedAt);
