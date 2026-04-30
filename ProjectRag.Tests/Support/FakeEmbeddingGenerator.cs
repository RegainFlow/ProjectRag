using Microsoft.Extensions.AI;

namespace ProjectRag.Tests.Support;

public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
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
        var vector = new float[768];
        var normalized = value.ToLowerInvariant();

        if (normalized.Contains("late") ||
            normalized.Contains("payment") ||
            normalized.Contains("invoice") ||
            normalized.Contains("fee"))
        {
            vector[0] = 1f;
            return vector;
        }

        if (normalized.Contains("security") ||
            normalized.Contains("password") ||
            normalized.Contains("multi-factor") ||
            normalized.Contains("mfa"))
        {
            vector[1] = 1f;
            return vector;
        }

        if (normalized.Contains("refund") ||
            normalized.Contains("credit"))
        {
            vector[2] = 1f;
            return vector;
        }

        vector[0] = 0.1f;
        vector[1] = 0.1f;
        vector[2] = 0.1f;

        return vector;
    }
}
