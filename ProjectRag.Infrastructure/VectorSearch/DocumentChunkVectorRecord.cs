
using Microsoft.Extensions.VectorData;

namespace ProjectRag.Infrastructure.VectorSearch;

internal sealed record DocumentChunkVectorRecord
{
    public const int EmbeddingDimensions = 768;

    [VectorStoreKey]
    public string ChunkId { get; set; } = "";

    [VectorStoreData]
    public string DocumentId { get; set; } = "";

    [VectorStoreData]
    public string SourceUri { get; set; } = "";

    [VectorStoreData]
    public string Text { get; set; } = "";

    [VectorStoreData]
    public string EmbeddingModel { get; set; } = "";

    [VectorStoreVector(EmbeddingDimensions, DistanceFunction = DistanceFunction.CosineDistance)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}