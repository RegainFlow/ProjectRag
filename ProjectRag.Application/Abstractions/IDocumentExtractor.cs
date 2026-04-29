using ProjectRag.Application.Models;

namespace ProjectRag.Application.Abstractions;

public interface IDocumentExtractor
{
    Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken cancellationToken);
}
