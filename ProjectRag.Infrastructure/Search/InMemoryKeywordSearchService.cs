using Microsoft.EntityFrameworkCore;
using ProjectRag.Application.Abstractions;
using ProjectRag.Application.Models;
using ProjectRag.Domain.Entities;

namespace ProjectRag.Infrastructure.Search;

internal sealed class InMemoryKeywordSearchService : IKeywordSearchService
{
    private readonly RagDbContext _db;

    public InMemoryKeywordSearchService(RagDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SearchHit>> SearchAsync(string query, int topK, SearchFilters? filters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        topK = Math.Clamp(topK, 1, 20);

        var terms = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .ToArray();

        var chunkQuery = _db.DocumentChunks
            .AsNoTracking()
            .Include(x => x.Document)
            .AsQueryable();

        chunkQuery = ApplyFilters(chunkQuery, filters);

        var chunks = await chunkQuery.ToListAsync(cancellationToken);

        return chunks
            .Select(chunk =>
            {
                var text = $"{chunk.Document?.Title} {chunk.SectionTitle} {chunk.Text}".ToLowerInvariant();
                var score = terms.Count(term => text.Contains(term));

                return new
                {
                    Chunk = chunk,
                    Score = score
                };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new SearchHit(
                x.Chunk.DocumentId,
                x.Chunk.Id,
                x.Chunk.Document?.SourceUri ?? "",
                x.Chunk.Text,
                x.Score,
                x.Chunk.PageNumber,
                x.Chunk.Kind,
                x.Chunk.SectionTitle,
                KeywordScore: x.Score,
                MatchedBy: "keyword"))
            .ToList();
    }

    private static IQueryable<DocumentChunk> ApplyFilters(IQueryable<DocumentChunk> query, SearchFilters? filters)
    {
        if (filters is null)
        {
            return query;
        }

        if (!string.IsNullOrWhiteSpace(filters.SourceType))
        {
            query = query.Where(x => x.Document != null && x.Document.SourceType == filters.SourceType);
        }

        if (!string.IsNullOrWhiteSpace(filters.SourceUriContains))
        {
            query = query.Where(x => x.Document != null && x.Document.SourceUri.Contains(filters.SourceUriContains));
        }

        if (filters.CreatedFrom is not null)
        {
            query = query.Where(x => x.Document != null && x.Document.CreatedAt >= filters.CreatedFrom.Value);
        }

        if (filters.CreatedTo is not null)
        {
            query = query.Where(x => x.Document != null && x.Document.CreatedAt <= filters.CreatedTo.Value);
        }

        if (filters.PageFrom is not null)
        {
            query = query.Where(x => x.Document != null && x.PageNumber >= filters.PageFrom.Value);
        }

        if (filters.PageTo is not null)
        {
            query = query.Where(x => x.Document != null && x.PageNumber <= filters.PageTo.Value);
        }

        return query;
    }
}
