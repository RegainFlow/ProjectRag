namespace ProjectRag.Application.Models;

public sealed record AnswerClaim(
    string Text,
    IReadOnlyList<Guid> CitationChunkIds);