namespace ProjectRag.Contracts;

public sealed record AskRequest(
    string Question,
    int TopK = 10,
    SearchFiltersRequest? Filters = null);

public sealed record AskResponse(
    string Answer,
    IReadOnlyList<CitationResponse> Citations);

public sealed record CitationResponse(
    string DocumentId,
    string ChunkId,
    string SourceUri,
    int? PageNumber,
    double Score,
    string Kind,
    string? SectionTitle);
