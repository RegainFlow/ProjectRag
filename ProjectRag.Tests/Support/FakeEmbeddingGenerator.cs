using Microsoft.Extensions.AI;

namespace ProjectRag.Tests.Support;

public class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = values.Select(value => new Embedding<float>(CreateVector(value))).ToList();

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
    }

    private static ReadOnlyMemory<float> CreateVector(string value)
    {
        var normalized = value.ToLowerInvariant();

        if (normalized.Contains("late") ||
            normalized.Contains("payment") ||
            normalized.Contains("invoice") ||
            normalized.Contains("fee"))
        {
            return new[] { 1f, 0f, 0f };
        }

        if (normalized.Contains("security") ||
            normalized.Contains("password") ||
            normalized.Contains("multi-factor") ||
            normalized.Contains("mfa"))
        {
            return new[] { 0f, 1f, 0f };
        }

        if (normalized.Contains("refund") ||
            normalized.Contains("credit"))
        {
            return new[] { 0f, 0f, 1f };
        }

        return new[] { 0.1f, 0.1f, 0.1f };
    }
}
