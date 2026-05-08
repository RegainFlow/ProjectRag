namespace ProjectRag.Tests.Evaluation;

public sealed record EvalCaseResult(
    string Id,
    bool RetrievalHit,
    bool CitationCorrect,
    bool AnswerStatusCorrect,
    TimeSpan Latency);