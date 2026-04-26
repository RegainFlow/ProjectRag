namespace ProjectRag.Contracts;

public sealed record StartIngestionRequest(
    string SourcePath);

public sealed record IngestionJobResponse(
    Guid IngestionId,
    string SourcePath,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);
public sealed record DocumentSummaryResponse(
    string DocumentId,
    string SourceUri,
    string Title,
    DateTimeOffset CreatedAt);

public sealed record SearchRequest(
    string Query,
    int TopK = 10);

public sealed record SearchResponse(
    string Query,
    IReadOnlyList<SearchHitResponse> Results);

public sealed record SearchHitResponse(
    string ChunkId,
    string DocumentId,
    string SourceUri,
    string TextPreview,
    double Score);

public sealed record AskRequest(
    string Question,
    int TopK = 10);

public sealed record AskResponse(
    string Answer,
    IReadOnlyList<CitationResponse> Citations);

public sealed record CitationResponse(
    string DocumentId,
    string ChunkId,
    string SourceUri,
    int? PageNumber,
    double Score);