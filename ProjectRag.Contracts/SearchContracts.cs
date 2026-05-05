namespace ProjectRag.Contracts;

public sealed record SearchFiltersRequest(
    string? SourceType = null,
    string? SourceUriContains = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    int? PageFrom = null,
    int? PageTo = null);

public sealed record SearchRequest(
    string Query,
    int TopK = 10,
    SearchFiltersRequest? Filters = null);

public sealed record SearchResponse(
    string Query,
    QueryRewriteResponse QueryRewrite,
    IReadOnlyList<SearchHitResponse> Results);

public sealed record SearchHitResponse(
    string ChunkId,
    string DocumentId,
    string SourceUri,
    string TextPreview,
    double RrfScore,
    int? PageNumber,
    string Kind,
    string? SectionTitle,
    double? VectorScore,
    double? KeywordScore,
    string MatchedBy);