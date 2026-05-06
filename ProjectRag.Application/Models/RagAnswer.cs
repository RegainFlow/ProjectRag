namespace ProjectRag.Application.Models;

public sealed record RagAnswer(
    string Answer,
    string AnswerStatus,
    QueryRewrite QueryRewrite,
    IReadOnlyList<AnswerClaim> Claims,
    IReadOnlyList<Citation> Citations,
    RetrievalDiagnostics RetrievalDiagnostics,
    ModelInfo ModelInfo);