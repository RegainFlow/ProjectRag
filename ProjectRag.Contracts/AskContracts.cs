namespace ProjectRag.Contracts;

public sealed record AskRequest(
    string Question,
    int TopK = 10,
    SearchFiltersRequest? Filters = null);

public sealed record AskResponse(
    string Answer,
    QueryRewriteResponse QueryRewrite,
    IReadOnlyList<CitationResponse> Citations);

public sealed record CitationResponse(
    string DocumentId,
    string ChunkId,
    string SourceUri,
    int? PageNumber,
    double RrfScore,
    double? VectorScore,
    double? KeywordScore,
    string MatchedBy,
    string Kind,
    string? SectionTitle);