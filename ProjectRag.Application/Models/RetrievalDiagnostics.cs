namespace ProjectRag.Application.Models;

public sealed record RetrievalDiagnostics(
    int RequestedTopK,
    int ReturnedContextCount,
    bool RerankingApplied);