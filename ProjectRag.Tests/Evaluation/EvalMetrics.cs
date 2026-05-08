namespace ProjectRag.Tests.Evaluation;

internal static class EvalMetrics
{
    public static EvalRunSummary Summarize(IReadOnlyList<EvalCaseResult> results)
    {
        return new EvalRunSummary(
            Total: results.Count,
            RetrievalHits: results.Count(x => x.RetrievalHit),
            CorrectCitations: results.Count(x => x.CitationCorrect),
            CorrectAnswerStatuses: results.Count(x => x.AnswerStatusCorrect),
            TotalLatency: TimeSpan.FromMilliseconds(results.Sum(x => x.Latency.TotalMilliseconds)));
    }
}
