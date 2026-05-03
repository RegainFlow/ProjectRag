namespace ProjectRag.Contracts;

public sealed record QueryRewriteResponse(
    string OriginalQuery,
    string SemanticQuery,
    string KeywordQuery,
    string Status);