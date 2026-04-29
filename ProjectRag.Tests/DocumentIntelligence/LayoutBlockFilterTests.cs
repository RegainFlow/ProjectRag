using ProjectRag.Infrastructure.DocumentIntelligence;

namespace ProjectRag.Tests.DocumentIntelligence;

public sealed class LayoutBlockFilterTests
{
    [Theory]
    [InlineData("pageHeader")]
    [InlineData("pageFooter")]
    [InlineData("pageNumber")]
    [InlineData("PAGEHEADER")]
    public void ShouldSkipLayoutRole_returns_true_for_low_value_roles(string role)
    {
        Assert.True(LayoutBlockFilter.ShouldSkipLayoutRole(role));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("title")]
    [InlineData("sectionHeading")]
    public void ShouldSkipLayoutRole_returns_false_for_useful_roles(string? role)
    {
        Assert.False(LayoutBlockFilter.ShouldSkipLayoutRole(role));
    }

    [Fact]
    public void Overlaps_returns_true_when_spans_intersect()
    {
        var left = new TextSpanRange(10, 10);  // 10-20
        var right = new TextSpanRange(15, 10); // 15-25

        Assert.True(LayoutBlockFilter.Overlaps(left, right));
    }

    [Fact]
    public void Overlaps_returns_false_when_spans_touch_but_do_not_intersect()
    {
        var left = new TextSpanRange(10, 10);  // 10-20
        var right = new TextSpanRange(20, 10); // 20-30

        Assert.False(LayoutBlockFilter.Overlaps(left, right));
    }

    [Fact]
    public void OverlapsAnyTableSpan_returns_true_when_paragraph_span_intersects_table_span()
    {
        var paragraphSpans = new[]
        {
              new TextSpanRange(50, 10)
          };

        var tableSpans = new[]
        {
              new TextSpanRange(40, 20)
          };

        Assert.True(LayoutBlockFilter.OverlapsAnyTableSpan(paragraphSpans, tableSpans));
    }
}
