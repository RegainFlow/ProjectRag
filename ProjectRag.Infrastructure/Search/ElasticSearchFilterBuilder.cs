using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ProjectRag.Application.Models;

namespace ProjectRag.Infrastructure.Search;

internal static class ElasticSearchFilterBuilder
{
    public static ICollection<Query> Build(SearchFilters? filters)
    {
        var queries = new List<Query>();

        if (filters is null)
        {
            return queries;
        }

        if (!string.IsNullOrWhiteSpace(filters.SourceType))
        {
            queries.Add(new TermQuery
            {
                Field = new Field("sourceType"),
                Value = filters.SourceType
            });
        }

        if (!string.IsNullOrWhiteSpace(filters.SourceUriContains))
        {
            queries.Add(new WildcardQuery
            {
                Field = new Field("sourceUri"),
                Value = $"*{filters.SourceUriContains}*",
                CaseInsensitive = true
            });
        }

        if (filters.CreatedFrom is not null || filters.CreatedTo is not null)
        {
            queries.Add(new DateRangeQuery
            {
                Field = new Field("createdAt"),
                Gte = filters.CreatedFrom,
                Lte = filters.CreatedTo
            });
        }

        if (filters.PageFrom is not null || filters.PageTo is not null)
        {
            queries.Add(new NumberRangeQuery
            {
                Field = new Field("pageNumber"),
                Gte = filters.PageFrom,
                Lte = filters.PageTo
            });
        }

        return queries;
    }
}