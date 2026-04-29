using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.Options;

namespace ProjectRag.Infrastructure.DocumentIntelligence;

internal sealed class AzureDocumentIntelligenceExtractor : IDocumentExtractor
{
    private readonly DocumentIntelligenceClient _client;
    private readonly DocumentIntelligenceOptions _options;

    public AzureDocumentIntelligenceExtractor(
        DocumentIntelligenceClient client,
        IOptions<DocumentIntelligenceOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<ExtractedDocument> ExtractAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var data = BinaryData.FromBytes(bytes);

        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            _options.ModelId,
            data,
            cancellationToken);

        var result = operation.Value;

        var rawBlocks = new List<RawLayoutBlock>();

        var tableSpans = result.Tables
            .SelectMany(table => table.Spans)
            .Select(span => new TextSpanRange(span.Offset, span.Length))
            .ToList();

        // extract paragraphs
        foreach (var paragraph in result.Paragraphs ?? [])
        {
            var text = paragraph.Content?.Trim();

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            int? pageNumber = paragraph.BoundingRegions.Count > 0 ? paragraph.BoundingRegions[0].PageNumber : null;

            var layoutRole = paragraph.Role?.ToString();
            if (LayoutBlockFilter.ShouldSkipLayoutRole(layoutRole))
            {
                continue;
            }

            var sourceOffset = paragraph.Spans.Count > 0 ? paragraph.Spans[0].Offset : int.MaxValue;
            var sourceLength = paragraph.Spans.Count > 0 ? paragraph.Spans[0].Length : 0;

            var paragraphSpans = paragraph.Spans
                .Select(span => new TextSpanRange(span.Offset, span.Length))
                .ToList();

            if (LayoutBlockFilter.OverlapsAnyTableSpan(paragraphSpans, tableSpans))
            {
                continue;
            }

            rawBlocks.Add(new RawLayoutBlock(
                SourceOffset: sourceOffset,
                SourceLength: sourceLength,
                Text: text,
                Kind: IsHeadingRole(layoutRole) ? ChunkKind.Heading : ChunkKind.Paragraph,
                PageNumber: pageNumber,
                SectionTitle: IsHeadingRole(layoutRole) ? text : null,
                LayoutRole: layoutRole,
                BoundingRegions: ToBoundingRegions(paragraph.BoundingRegions)));
        }

        // extract tables
        foreach (var table in result.Tables ?? [])
        {
            var text = ConvertTableToMarkdown(table);

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            int? pageNumber = table.BoundingRegions.Count > 0 ? table.BoundingRegions[0].PageNumber : null;
            var sourceOffset = table.Spans.Count > 0 ? table.Spans[0].Offset : int.MaxValue;
            var sourceLength = table.Spans.Count > 0 ? table.Spans[0].Length : 0;

            rawBlocks.Add(new RawLayoutBlock(
                SourceOffset: sourceOffset,
                SourceLength: sourceLength,
                Text: text,
                Kind: ChunkKind.Table,
                PageNumber: pageNumber,
                SectionTitle: null,
                LayoutRole: "table",
                BoundingRegions: ToBoundingRegions(table.BoundingRegions)));
        }

        return new ExtractedDocument(
            SourcePath: filePath,
            Blocks: new LayoutBlockNormalizer().Normalize(rawBlocks));
    }

    private static string ConvertTableToMarkdown(DocumentTable table)
    {
        if (table.Cells.Count == 0)
        {
            return "";
        }

        var rows = table.Cells
            .GroupBy(cell => cell.RowIndex)
            .OrderBy(group => group.Key)
            .Select(group => group
                .OrderBy(cell => cell.ColumnIndex)
                .Select(cell => NormalizeCellText(cell.Content))
                .ToList())
            .ToList();

        if (rows.Count == 0)
        {
            return "";
        }

        var maxColumns = rows.Max(row => row.Count);

        foreach (var row in rows)
        {
            while (row.Count < maxColumns)
            {
                row.Add("");
            }
        }

        var lines = new List<string>
        {
            "| " + string.Join(" | ", rows[0]) + " |",
            "| " + string.Join(" | ", Enumerable.Repeat("---", maxColumns)) + " |"
        };

        foreach (var row in rows.Skip(1))
        {
            lines.Add("| " + string.Join(" | ", row) + " |");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string NormalizeCellText(string? text)
    {
        return string.IsNullOrWhiteSpace(text) ? "" : text.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static bool IsHeadingRole(string? role)
    {
        return string.Equals(role, "title", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "sectionHeading", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ExtractedBoundingRegion> ToBoundingRegions(IReadOnlyList<BoundingRegion> boundingRegions)
    {
        if (boundingRegions.Count == 0)
        {
            return [];
        }

        return boundingRegions
            .Select(region => new ExtractedBoundingRegion(
                region.PageNumber,
                region.Polygon?.Select(point => point).ToList() ?? []))
            .ToList();
    }
}