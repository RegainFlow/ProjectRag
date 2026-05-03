namespace ProjectRag.Application.Models;

public sealed record RetrievalQuery(
    string OriginalQuery,
    string SemanticQuery,
    string KeywordQuery);
