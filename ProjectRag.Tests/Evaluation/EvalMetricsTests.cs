namespace ProjectRag.Tests.Evaluation;

public sealed class EvalMetricsTests
{
    [Fact]
    public void Summarize_calculates_rates_and_average_latency()
    {
        var summary = EvalMetrics.Summarize(
        [
            new EvalCaseResult(
                  Id: "one",
                  RetrievalHit: true,
                  CitationCorrect: true,
                  AnswerStatusCorrect: true,
                  Latency: TimeSpan.FromMilliseconds(100)),
              new EvalCaseResult(
                  Id: "two",
                  RetrievalHit: false,
                  CitationCorrect: false,
                  AnswerStatusCorrect: true,
                  Latency: TimeSpan.FromMilliseconds(300))
        ]);

        Assert.Equal(2, summary.Total);
        Assert.Equal(1, summary.RetrievalHits);
        Assert.Equal(1, summary.CorrectCitations);
        Assert.Equal(2, summary.CorrectAnswerStatuses);

        Assert.Equal(0.5, summary.RetrievalHitRate);
        Assert.Equal(0.5, summary.CitationCorrectness);
        Assert.Equal(1.0, summary.AnswerStatusAccuracy);
        Assert.Equal(TimeSpan.FromMilliseconds(200), summary.AverageLatency);
    }
}
