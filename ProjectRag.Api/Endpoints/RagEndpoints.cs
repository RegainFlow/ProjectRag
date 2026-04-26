using Microsoft.EntityFrameworkCore;
using ProjectRag.Contracts;
using ProjectRag.Domain.Entities;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure;

namespace ProjectRag.Api.Endpoints;

public static class RagEndpoints
{
    public static RouteGroupBuilder MapRagEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/health", () =>
        Results.Ok(new
        {
            Status = "Healthy",
            Service = "ProjectRag.Api",
            TimeUtc = DateTime.UtcNow
        }));

        group.MapPost("/ingestions", async (
            StartIngestionRequest request,
            RagDbContext db,
            CancellationToken cancellationToken) =>
        {
            var job = new IngestionJob
            {
                Id = Guid.NewGuid(),
                SourcePath = request.SourcePath,
                Status = IngestionJobStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            db.IngestionJobs.Add(job);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Accepted($"/api/v1/ingestions/{job.Id}", new IngestionJobResponse
            (
                job.Id,
                job.SourcePath,
                job.Status.ToString(),
                job.ErrorMessage,
                job.CreatedAt,
                job.StartedAt,
                job.CompletedAt
            ));
        });

        group.MapGet("/ingestions/{id:guid}", async (
            Guid id,
            RagDbContext db,
            CancellationToken cancellationToken) =>
        {
            var job = await db.IngestionJobs
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new IngestionJobResponse
                (
                    x.Id,
                    x.SourcePath,
                    x.Status.ToString(),
                    x.ErrorMessage,
                    x.CreatedAt,
                    x.StartedAt,
                    x.CompletedAt
                ))
                .SingleOrDefaultAsync(cancellationToken);

            return job is null ? Results.NotFound() : Results.Ok(job);
        });

        group.MapGet("/documents", async (
            RagDbContext db,
            CancellationToken cancellationToken) =>
        {
            var documents = await db.Documents
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DocumentSummaryResponse(
                x.Id.ToString(),
                x.SourceUri,
                x.Title,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

            return Results.Ok(documents);
        });

        group.MapPost("/search", (SearchRequest request) =>
            Results.Ok(new SearchResponse(
                request.Query,
                Array.Empty<SearchHitResponse>())));

        group.MapPost("/ask", (AskRequest request) =>
        Results.Ok(new AskResponse(
            "Phase 0 placeholder: RAG answer generation is not implemented yet.",
            Array.Empty<CitationResponse>())));

        return group;
    }
}
