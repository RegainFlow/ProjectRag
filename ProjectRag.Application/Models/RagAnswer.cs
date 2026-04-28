namespace ProjectRag.Application.Models;

public sealed record RagAnswer(
    string Answer,
    IReadOnlyList<Citation> Citations);