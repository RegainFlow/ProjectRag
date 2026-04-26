# ProjectRag

ProjectRag is a learning-first .NET RAG service. The project is being built in phases so each layer introduces one production retrieval-augmented generation capability at a time.

Current status: Phase 0 foundation is implemented. The API shell, EF Core SQLite persistence, migrations, placeholder RAG endpoints, and initial integration tests are in place.

## Phase Roadmap

1. Phase 0: .NET Minimal API foundation, EF Core, SQLite, OpenAPI, clean layering.
2. Phase 1: Naive vector-only RAG over plain text or markdown documents.
3. Phase 2: Scanned PDF ingestion with Azure AI Document Intelligence.
4. Phase 3: Persistent local RAG and idempotent ingestion.
5. Phase 4+: Hybrid retrieval, query rewriting, RRF fusion, reranking, grounded answers, evaluation, and agentic RAG.

## Solution Layout

```text
ProjectRag.Api                 Minimal API endpoints and composition root
ProjectRag.Application         Application services and orchestration, added as phases grow
ProjectRag.Contracts           API request/response DTOs
ProjectRag.Domain              Domain entities and enums
ProjectRag.Infrastructure      EF Core, SQLite, migrations, persistence configuration
ProjectRag.Ingestion.Worker    Background worker shell for future ingestion processing
ProjectRag.Tests               Unit and integration tests
docs                           Architecture and agent guidance
```

## Current API Surface

Base route:

```text
/api/v1
```

Implemented endpoints:

```text
GET  /health
POST /ingestions
GET  /ingestions/{id}
GET  /documents
POST /search
POST /ask
```

`/search` and `/ask` are still placeholders for Phase 1.

## Prerequisites

- .NET 10 SDK
- EF Core CLI tool matching the EF Core package major/minor used by the project

Recommended EF tool setup:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.7
```

## Local Development

Restore and build:

```powershell
dotnet restore
dotnet build ProjectRag.slnx
```

Run tests:

```powershell
dotnet test ProjectRag.Tests\ProjectRag.Tests.csproj
```

Apply local SQLite migrations:

```powershell
dotnet ef database update `
  --project .\ProjectRag.Infrastructure\ProjectRag.Infrastructure.csproj `
  --startup-project .\ProjectRag.Api\ProjectRag.Api.csproj
```

Run the API:

```powershell
dotnet run --project ProjectRag.Api
```

OpenAPI is mapped in development with `MapOpenApi()`.

## Configuration

Local development uses SQLite:

```json
"ConnectionStrings": {
  "ProjectRagDb": "Data Source=projectrag.dev.sqlite"
}
```

The SQLite database file is local runtime state and should not be committed. Secrets should not be stored in committed `appsettings` files. Use user-secrets, environment variables, or deployment secret stores for credentials.

## References

- ASP.NET Core Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
- ASP.NET Core OpenAPI: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi
- EF Core SQLite provider: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/
- EF Core migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
