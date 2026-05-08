namespace ProjectRag.Tests.Evaluation;

public sealed record EvalRunSummary(
    int Total,
    int RetrievalHits,
    int CorrectCitations,
    int CorrectAnswerStatuses,
    TimeSpan TotalLatency)
{
    public double RetrievalHitRate => Total == 0 ? 0 : (double)RetrievalHits / Total;

    public double CitationCorrectness => Total == 0 ? 0 : (double)CorrectCitations / Total;

    public double AnswerStatusAccuracy => Total == 0 ? 0 : (double)CorrectAnswerStatuses / Total;

    public TimeSpan AverageLatency => Total == 0
        ? TimeSpan.Zero
        : TimeSpan.FromMilliseconds(TotalLatency.TotalMilliseconds / Total);
}