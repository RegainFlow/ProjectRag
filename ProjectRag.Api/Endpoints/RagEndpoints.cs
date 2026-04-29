using Microsoft.EntityFrameworkCore;
using ProjectRag.Application.Abstractions;
using ProjectRag.Contracts;
using ProjectRag.Domain.Entities;
using ProjectRag.Domain.Enums;
using ProjectRag.Infrastructure;

namespace ProjectRag.Api.Endpoints;

internal static class RagEndpoints
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
            ITextDocumentIngestionService ingestionService,
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

            try
            {
                job.Status = IngestionJobStatus.Running;
                job.StartedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);

                await ingestionService.IngestPathAsync(job.SourcePath, cancellationToken);

                job.Status = IngestionJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                job.Status = IngestionJobStatus.Failed;
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

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

        group.MapPost("/search", async (
            SearchRequest request,
            IVectorSearchService vectorSearch,
            CancellationToken cancellationToken) =>
        {
            var hits = await vectorSearch.SearchAsync(request.Query, request.TopK, cancellationToken);

            var response = new SearchResponse(
                request.Query,
                hits.Select(x => new SearchHitResponse(
                    x.ChunkId.ToString(),
                    x.DocumentId.ToString(),
                    x.Source,
                    x.Text.Length <= 300 ? x.Text : x.Text[..300], // limit text preview size to 300
                    x.Score,
                    x.PageNumber,
                    x.Kind.ToString(),
                    x.SectionTitle)).ToList());

            return Results.Ok(response);
        });


        group.MapPost("/ask", async (
            AskRequest request,
            IRagAnswerService ragAnswerService,
            CancellationToken cancellationToken) =>
        {
            var answer = await ragAnswerService.AnswerAsync(request.Question, request.TopK, cancellationToken);

            var response = new AskResponse(
                answer.Answer,
                answer.Citations.Select(x => new CitationResponse(
                    x.DocumentId.ToString(),
                    x.ChunkId.ToString(),
                    x.Source,
                    x.PageNumber,
                    x.Score,
                    x.Kind.ToString(),
                    x.SectionTitle)).ToList());

            return Results.Ok(response);
        });

        return group;
    }
}
