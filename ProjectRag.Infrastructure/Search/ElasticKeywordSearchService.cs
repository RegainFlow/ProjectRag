using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure.Options;

namespace ProjectRag.Infrastructure.Search;

internal sealed class ElasticKeywordSearchService : IKeywordSearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;

    public ElasticKeywordSearchService(
        ElasticsearchClient client,
        IOptions<ElasticsearchOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, SearchFilters? filters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        topK = Math.Clamp(topK, 1, 20);

        var filterQueries = ElasticSearchFilterBuilder.Build(filters);

        var response = await _client.SearchAsync<ElasticDocumentChunkRecord>(
            descriptor => descriptor
                .Indices(_options.IndexName)
                .Size(topK)
                .Query(q => q
                    .Bool(b => b
                        .Must(must => must
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(new[]
                                {
                                    Infer.Field<ElasticDocumentChunkRecord>(x => x.Text),
                                    Infer.Field<ElasticDocumentChunkRecord>(x => x.SectionTitle),
                                    Infer.Field<ElasticDocumentChunkRecord>(x => x.Title),
                                })))
                        .Filter(filterQueries.ToArray()))),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException("Elasticsearch keyword search failed.");
        }

        return response.Hits
            .Where(hit => hit.Source is not null)
            .Select(hit =>
            {
                var source = hit.Source!;

                return new SearchHit(
                    Guid.Parse(source.DocumentId),
                    Guid.Parse(source.ChunkId),
                    source.SourceUri,
                    source.Text,
                    hit.Score ?? 0,
                    source.PageNumber,
                    Enum.TryParse<ChunkKind>(source.Kind, out var kind) ? kind : ChunkKind.Unknown,
                    source.SectionTitle,
                    KeywordScore: hit.Score ?? 0,
                    MatchedBy: "keyword");
            })
            .ToList();
    }
}
