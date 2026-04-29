using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.DocumentIntelligence;

namespace ProjectRag.Tests.DocumentIntelligence;

public sealed class LayoutBlockNormalizerTests
{
    [Fact]
    public void Normalize_merges_paragraphs_under_current_heading()
    {
        var normalizer = new LayoutBlockNormalizer();

        var blocks = new[]
        {
            Block(0, "3. Others", ChunkKind.Heading, sectionTitle: "3. Others"),
            Block(10, "AI Document Intelligence is an AI service.", ChunkKind.Paragraph),
            Block(60, "It extracts text and tables from documents.", ChunkKind.Paragraph)
        };

        var result = normalizer.Normalize(blocks);

        Assert.Equal(2, result.Count);
        Assert.Equal(ChunkKind.Heading, result[0].Kind);
        Assert.Equal(ChunkKind.Paragraph, result[1].Kind);
        Assert.Equal("3. Others", result[1].SectionTitle);
        Assert.Contains("AI Document Intelligence", result[1].Text);
        Assert.Contains("It extracts text", result[1].Text);
    }

    [Fact]
    public void Normalize_keeps_tables_separate()
    {
        var normalizer = new LayoutBlockNormalizer();

        var blocks = new[]
        {
              Block(0, "2.1 Table", ChunkKind.Heading, sectionTitle: "2.1 Table"),
              Block(10, "Before table paragraph.", ChunkKind.Paragraph),
              Block(40, "| Name | Corp |\n| --- | --- |", ChunkKind.Table),
              Block(90, "After table paragraph.", ChunkKind.Paragraph)
          };

        var result = normalizer.Normalize(blocks);

        Assert.Contains(result, x => x.Kind == ChunkKind.Table);
        Assert.Equal("2.1 Table", result.Single(x => x.Kind == ChunkKind.Table).SectionTitle);
    }

    [Fact]
    public void Normalize_preserves_page_number_and_bounding_regions_for_merged_paragraphs()
    {
        var normalizer = new LayoutBlockNormalizer();

        var blocks = new[]
        {
            Block(
                0,
                "3. Others",
                ChunkKind.Heading,
                sectionTitle: "3. Others"),
            new RawLayoutBlock(
                SourceOffset: 10,
                SourceLength: 20,
                Text: "First paragraph in the section.",
                Kind: ChunkKind.Paragraph,
                PageNumber: 2,
                SectionTitle: null,
                LayoutRole: null,
                BoundingRegions:
                [
                    new(2, [0, 0, 1, 0, 1, 1, 0, 1])
                ]),
            new RawLayoutBlock(
                SourceOffset: 40,
                SourceLength: 20,
                Text: "Second paragraph in the same section.",
                Kind: ChunkKind.Paragraph,
                PageNumber: 2,
                SectionTitle: null,
                LayoutRole: null,
                BoundingRegions:
                [
                    new(2, [1, 1, 2, 1, 2, 2, 1, 2])
                ])
        };

        var result = normalizer.Normalize(blocks);

        var paragraph = Assert.Single(result, x => x.Kind == ChunkKind.Paragraph);

        Assert.Equal(2, paragraph.PageNumber);
        Assert.Equal("3. Others", paragraph.SectionTitle);
        Assert.Equal(2, paragraph.BoundingRegions.Count);
    }

    private static RawLayoutBlock Block(
        int offset,
        string text,
        ChunkKind kind,
        string? sectionTitle = null)
    {
        return new RawLayoutBlock(
            SourceOffset: offset,
            SourceLength: text.Length,
            Text: text,
            Kind: kind,
            PageNumber: 1,
            SectionTitle: sectionTitle,
            LayoutRole: kind == ChunkKind.Heading ? "sectionHeading" : null,
            BoundingRegions: []);
    }
}
