namespace ProjectRag.Infrastructure.DocumentIntelligence;

internal sealed record TextSpanRange(int Offset, int Length)
{
    public int End => Offset + Length;
}
