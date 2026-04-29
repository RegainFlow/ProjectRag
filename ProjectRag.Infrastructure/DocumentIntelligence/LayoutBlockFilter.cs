namespace ProjectRag.Infrastructure.DocumentIntelligence;

internal static class LayoutBlockFilter
{
    public static bool ShouldSkipLayoutRole(string? role)
    {
        return string.Equals(role, "pageHeader", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "pageFooter", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "pageNumber", StringComparison.OrdinalIgnoreCase);
    }

    public static bool Overlaps(TextSpanRange left, TextSpanRange right)
    {
        return left.Offset < right.End && right.Offset < left.End;
    }

    public static bool OverlapsAnyTableSpan(
        IReadOnlyList<TextSpanRange> paragraphSpans,
        IReadOnlyList<TextSpanRange> tableSpans)
    {
        return paragraphSpans.Any(
            paragraphSpan => tableSpans.Any(
                tableSpan => Overlaps(paragraphSpan, tableSpan)));
    }
}
