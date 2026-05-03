namespace ProjectRag.Application.Models;

public sealed record QueryRewrite(
    string OriginalQuery,
    string SemanticQuery,
    string KeywordQuery,
    string Status);
