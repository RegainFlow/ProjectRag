namespace ProjectRag.Infrastructure.Options;

internal sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int CandidateCount { get; set; } = 30;
    public int MaxTopK { get; set; } = 20;
    public int RerankerMaxTextChars { get; set; } = 800;
}