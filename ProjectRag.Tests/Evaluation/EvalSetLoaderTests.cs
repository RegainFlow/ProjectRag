namespace ProjectRag.Tests.Evaluation;

public sealed class EvalSetLoaderTests
{
    [Fact]
    public void Load_returns_seed_cases()
    {
        var cases = EvalSetLoader.LoadEvalSet();

        Assert.NotEmpty(cases);
        Assert.Contains(cases, x => x.Id == "late-payment-fees");
        Assert.All(cases, x => Assert.False(string.IsNullOrWhiteSpace(x.Question)));
        Assert.All(cases, x => Assert.False(string.IsNullOrWhiteSpace(x.ExpectedAnswerStatus)));
    }
}
