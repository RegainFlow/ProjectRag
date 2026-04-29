using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;

namespace ProjectRag.Infrastructure.DocumentIntelligence;

internal sealed class LayoutBlockNormalizer
{
    private const int shortParagraphLength = 40;
    private const int maxMergedParagraphLength = 1_200;

    public IReadOnlyList<ExtractedBlock> Normalize(IReadOnlyList<RawLayoutBlock> rawBlocks)
    {
        var normalized = new List<ExtractedBlock>();
        var paragraphBuffer = new List<RawLayoutBlock>();
        var currentSectionTitle = default(string);

        foreach (var block in rawBlocks.OrderBy(x => x.SourceOffset))
        {
            if (block.Kind == ChunkKind.Heading)
            {
                FlushParagraphBuffer();
                currentSectionTitle = block.Text;

                normalized.Add(ToExtractedBlock(normalized.Count, block with { SectionTitle = block.Text }));

                continue;
            }

            if (block.Kind == ChunkKind.Table)
            {
                FlushParagraphBuffer();

                normalized.Add(ToExtractedBlock(normalized.Count, block with { SectionTitle = currentSectionTitle }));

                continue;
            }

            if (block.Text.Length < shortParagraphLength)
            {
                paragraphBuffer.Add(block);
                continue;
            }

            paragraphBuffer.Add(block);

            var totalLength = paragraphBuffer.Sum(x => x.Text.Length);
            if (totalLength >= maxMergedParagraphLength)
            {
                FlushParagraphBuffer();
            }
        }

        FlushParagraphBuffer();

        return normalized;

        void FlushParagraphBuffer()
        {
            if (paragraphBuffer.Count == 0)
            {
                return;
            }

            var text = string.Join(
                Environment.NewLine,
                paragraphBuffer.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(text))
            {
                var first = paragraphBuffer[0];

                normalized.Add(new ExtractedBlock(
                    BlockIndex: normalized.Count,
                    Text: text,
                    Kind: ChunkKind.Paragraph,
                    PageNumber: first.PageNumber,
                    SectionTitle: currentSectionTitle,
                    LayoutRole: null,
                    BoundingRegions: paragraphBuffer
                        .SelectMany(x => x.BoundingRegions)
                        .ToList()));
            }

            paragraphBuffer.Clear();
        }
    }

    private static ExtractedBlock ToExtractedBlock(int blockIndex, RawLayoutBlock block)
    {
        return new ExtractedBlock(
            BlockIndex: blockIndex,
            Text: block.Text,
            Kind: block.Kind,
            PageNumber: block.PageNumber,
            SectionTitle: block.SectionTitle,
            LayoutRole: block.LayoutRole,
            BoundingRegions: block.BoundingRegions);
    }
}