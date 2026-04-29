namespace ProjectRag.Application.Models;

public sealed record ExtractedDocument(
    string SourcePath,
    IReadOnlyList<ExtractedBlock> Blocks);
