namespace ProjectRag.Tests.Evaluation;

public sealed record EvalCase(
    string Id,
    string Question,
    string? ExpectedSourceContains,
    IReadOnlyList<string> ExpectedAnswerContains,
    string ExpectedAnswerStatus);