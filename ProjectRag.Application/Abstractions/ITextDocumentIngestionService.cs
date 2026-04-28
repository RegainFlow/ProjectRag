namespace ProjectRag.Application.Abstractions;

public interface ITextDocumentIngestionService
{
    Task IngestPathAsync(string sourcePath, CancellationToken cancellationToken);
}
