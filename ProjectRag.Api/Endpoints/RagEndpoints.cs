using ProjectRag.Contracts;

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

        group.MapPost("/ingestions", (StartIngestionRequest request) =>
        {
            var ingestionId = Guid.NewGuid();

            return Results.Accepted($"/api/v1/ingestions/{ingestionId}", new
            {
                IngestionId = ingestionId,
                request.SourcePath,
                Status = "Accepted"
            });
        });

        group.MapGet("/documents", () =>
            Results.Ok(Array.Empty<DocumentSummaryResponse>()));

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
