# Agent Guidance

This file gives AI assistants and coding agents project-specific instructions for working in ProjectRag.

## Role

Act as a mentor and implementation assistant for a learning-focused .NET RAG service. The human developer is the driver. Prefer explaining why a change is made, where it belongs, and how to verify it.

## Project Goals

ProjectRag is built in phases:

1. Foundation API and persistence.
2. Naive RAG.
3. Scanned PDF ingestion.
4. Persistent local retrieval with MEVD SQLiteVec.
5. Hybrid retrieval and ranking improvements.
6. Grounded answer generation.
7. Evaluation and agentic RAG.

Keep changes aligned with the active phase. Do not introduce later-phase abstractions before they are needed.

## Current Architecture

- `ProjectRag.Api` owns HTTP endpoints and the application composition root.
- `ProjectRag.Contracts` owns API DTOs.
- `ProjectRag.Domain` owns entities and enums.
- `ProjectRag.Infrastructure` owns EF Core, SQLite, migrations, persistence configuration, provider integrations, document extraction, chunking implementations, MEVD SQLiteVec vector indexing, and vector search implementations.
- `ProjectRag.Tests` owns test coverage.

Dependency direction should stay:

```text
Api -> Infrastructure -> Domain
Api -> Contracts
Application -> Domain
Infrastructure -> Application, Domain
```

Avoid references from `Domain` to EF Core, ASP.NET Core, Infrastructure, or API.

## Coding Conventions

- Prefer official Microsoft/.NET templates and documentation before hand-rolled conventions.
- Keep project visibility intentional:
  - Infrastructure concrete services/configurations/options should be `internal`.
  - Infrastructure DI entry point and `RagDbContext` should stay `public`.
  - Application abstractions/models should stay `public`.
  - Domain entities/enums should stay `public`.
  - API endpoint mapping classes should be `internal`.
- Keep domain entities plain C#.
- Use Fluent API configuration classes for EF mapping.
- Keep API DTOs separate from EF entities.
- Use `DateTime.UtcNow` for persisted timestamps while SQLite is the database because SQLite has provider limitations around ordering/comparing `DateTimeOffset`.
- Use `CancellationToken` in async endpoint/database operations.
- Use `AsNoTracking()` for read-only EF queries.
- Keep local SQLite files and secrets out of source control.
- Keep normal tests independent from Ollama by using fake `IEmbeddingGenerator` and `IChatClient` implementations.
- Keep normal tests independent from Azure by using fake `IDocumentExtractor` implementations.
- Keep MEVD provider records and SQLiteVec-specific types inside Infrastructure. Do not leak provider storage records into Domain, Contracts, or API responses.
- SQLiteVec is currently a preview connector. Keep its package version aligned with the `Microsoft.Extensions.VectorData.Abstractions` version it expects before upgrading vector packages.
- Do not commit real Azure keys, local scanned datasets, real customer documents, or local SQLite database files.

## Testing

For API persistence tests, use:

- `WebApplicationFactory<Program>`
- SQLite in-memory connection
- DI override for `RagDbContext`

Do not test against the developer's local SQLite database file.

For MEVD SQLiteVec integration tests, use file-backed temporary SQLite databases. In-memory SQLite is good for EF Core API tests, but SQLiteVec opens provider-managed connections, so a shared physical test file is the safer boundary.

When adding or changing behavior, update tests in the same change. This applies to new endpoints, services, contracts, persistence mappings, ingestion behavior, retrieval logic, and answer-generation logic. Prefer focused tests that prove the new behavior through the narrowest useful boundary. For API endpoints, add or update integration tests that exercise the HTTP route and verify the response contract.

When adding, removing, or changing API endpoints, update `ProjectRag.Api/ProjectRag.Api.http` in the same change with a simple runnable request example. Keep examples realistic, valid JSON, and aligned with the `/api/v1` route prefix.

When adding or changing extraction/chunking behavior, add focused tests that prove the behavior without calling Azure. Layout-aware scanned ingestion should preserve page number, section title, chunk kind, and bounding-region metadata when those values are available.

## Verification Commands

Run these before calling a phase complete:

```powershell
dotnet build ProjectRag.slnx
dotnet test ProjectRag.Tests\ProjectRag.Tests.csproj
```

For migrations:

```powershell
dotnet ef migrations list `
  --project .\ProjectRag.Infrastructure\ProjectRag.Infrastructure.csproj `
  --startup-project .\ProjectRag.Api\ProjectRag.Api.csproj
```

## Documentation Sources

When giving guidance, prefer official Microsoft documentation:

- ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/
- EF Core: https://learn.microsoft.com/en-us/ef/core/
- .NET CLI: https://learn.microsoft.com/en-us/dotnet/core/tools/
