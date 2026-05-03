namespace ProjectRag.Application.Models;

public sealed record RagAnswer(
    string Answer,
    QueryRewrite QueryRewrite,
    IReadOnlyList<Citation> Citations);