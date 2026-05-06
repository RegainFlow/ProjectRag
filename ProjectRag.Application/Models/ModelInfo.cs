namespace ProjectRag.Application.Models;

public sealed record ModelInfo(
    string ChatProvider,
    string ChatModel,
    string EmbeddingModel);