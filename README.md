# ProjectRag

ProjectRag is a learning-first .NET RAG service. The project is being built in phases so each layer introduces one production retrieval-augmented generation capability at a time.

Current status: Phase 2 scanned document ingestion is implemented. The API can ingest local text/markdown files plus scanned PDFs/images with Azure AI Document Intelligence, persist layout-aware chunks in SQLite, run vector search with Ollama embeddings, and generate grounded answers with Ollama chat.

## Phase Roadmap

1. Phase 0: .NET Minimal API foundation, EF Core, SQLite, OpenAPI, clean layering.
2. Phase 1: Naive vector-only RAG over plain text or markdown documents.
3. Phase 2: Scanned PDF/image ingestion with Azure AI Document Intelligence.
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

`/search` performs vector retrieval over ingested chunks. `/ask` retrieves relevant chunks, builds a grounded prompt, calls the configured chat model, and returns citations. Scanned document citations can include page and section metadata.

## Prerequisites

- .NET 10 SDK
- EF Core CLI tool matching the EF Core package major/minor used by the project
- Ollama running locally for Phase 1 embeddings and chat
- Azure AI Document Intelligence resource for Phase 2 scanned PDF/image ingestion

Recommended EF tool setup:

```powershell
dotnet tool update --global dotnet-ef --version 10.0.7
```

Recommended Ollama setup:

```powershell
ollama pull nomic-embed-text
ollama pull llama3.2
ollama list
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

Ingest the sample corpus with `ProjectRag.Api/ProjectRag.Api.http` or an HTTP client:

```json
{
  "sourcePath": "samples/docs"
}
```

Then search:

```json
{
  "query": "late payment fees",
  "topK": 5
}
```

Then ask:

```json
{
  "question": "What are the late payment fees?",
  "topK": 5
}
```

For scanned documents, place local PDF/image files under `ProjectRag.Api/samples/scanned/` and ingest:

```json
{
  "sourcePath": "samples/scanned"
}
```

Scanned sample files should stay local-only. The committed markdown files under `ProjectRag.Api/samples/docs/` are synthetic project-owned test data.

## Configuration

Local development uses SQLite:

```json
"ConnectionStrings": {
  "ProjectRagDb": "Data Source=projectrag.dev.sqlite"
}
```

Local AI uses Ollama:

```json
"AI": {
  "OllamaEndpoint": "http://localhost:11434",
  "ChatModel": "llama3.2",
  "EmbeddingModel": "nomic-embed-text",
  "TimeoutSeconds": 300
}
```

Azure AI Document Intelligence uses local secrets for real credentials:

```json
"DocumentIntelligence": {
  "Endpoint": "",
  "ApiKey": "",
  "ModelId": "prebuilt-layout"
}
```

Set local secrets from the API project:

```powershell
dotnet user-secrets init --project .\ProjectRag.Api\ProjectRag.Api.csproj
dotnet user-secrets set "DocumentIntelligence:Endpoint" "https://YOUR-RESOURCE.cognitiveservices.azure.com/" --project .\ProjectRag.Api\ProjectRag.Api.csproj
dotnet user-secrets set "DocumentIntelligence:ApiKey" "YOUR-KEY" --project .\ProjectRag.Api\ProjectRag.Api.csproj
```

The SQLite database file is local runtime state and should not be committed. Secrets should not be stored in committed `appsettings` files. Use user-secrets, environment variables, or deployment secret stores for credentials.

## References

- ASP.NET Core Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
- ASP.NET Core OpenAPI: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi
- EF Core SQLite provider: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/
- EF Core migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- Microsoft.Extensions.AI local AI quickstart with Ollama: https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/quickstart-local-ai
- Ollama embeddings: https://docs.ollama.com/capabilities/embeddings
- Azure AI Document Intelligence layout model: https://learn.microsoft.com/azure/ai-services/document-intelligence/prebuilt/layout
- ASP.NET Core app secrets: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
