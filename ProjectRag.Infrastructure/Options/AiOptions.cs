namespace ProjectRag.Infrastructure.Options;

internal sealed class AiOptions
{
    public const string SectionName = "AI";

    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "llama3.2";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int TimeoutSeconds { get; set; } = 300;
}
