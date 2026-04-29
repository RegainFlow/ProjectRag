using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;

namespace ProjectRag.Tests.Support;

public sealed class FakeDocumentExtractor : IDocumentExtractor
{
    public Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken)
    {
        var document = new ExtractedDocument(
            filePath,
            [
                new ExtractedBlock(
                    BlockIndex: 0,
                    Text: "Invoice 1001",
                    Kind: ChunkKind.Heading,
                    PageNumber: 1,
                    SectionTitle: "Invoice 1001",
                    LayoutRole: "title",
                    BoundingRegions:
                    [
                        new ExtractedBoundingRegion(
                            PageNumber: 1,
                            Polygon: [0, 0, 4, 0, 4, 1, 0, 1])
                    ]),

                new ExtractedBlock(
                    BlockIndex: 1,
                    Text: "Total amount due is 120 dollars.",
                    Kind: ChunkKind.Paragraph,
                    PageNumber: 1,
                    SectionTitle: null,
                    LayoutRole: null,
                    BoundingRegions:
                    [
                        new ExtractedBoundingRegion(
                            PageNumber: 1,
                            Polygon: [0, 1, 4, 1, 4, 2, 0, 2])
                    ])
            ]);

        return Task.FromResult(document);
    }
}