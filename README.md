# ProjectRag

ProjectRag is a learning-first .NET RAG service. The project is being built in phases so each layer introduces one production retrieval-augmented generation capability at a time.

Current status: Phase 4 hybrid retrieval is implemented. The API can ingest local text/markdown files plus scanned PDFs/images with Azure AI Document Intelligence, persist layout-aware chunks in SQLite, index chunk text and embeddings into Elasticsearch, run keyword + vector retrieval, merge candidates, and generate grounded answers with Ollama chat.

## Phase Roadmap

1. Phase 0: .NET Minimal API foundation, EF Core, SQLite, OpenAPI, clean layering.
2. Phase 1: Naive vector-only RAG over plain text or markdown documents.
3. Phase 2: Scanned PDF/image ingestion with Azure AI Document Intelligence.
4. Phase 3: Persistent local RAG with content-hash idempotency.
5. Phase 4: Elasticsearch hybrid keyword + vector retrieval with metadata filters.
6. Phase 5+: Query rewriting, RRF fusion, reranking, stricter grounded answers, evaluation, and agentic RAG.

## Solution Layout

```text
ProjectRag.Api                 Minimal API endpoints and composition root
ProjectRag.Application         Application services and orchestration, added as phases grow
ProjectRag.Contracts           API request/response DTOs
ProjectRag.Domain              Domain entities and enums
ProjectRag.Infrastructure      EF Core, SQLite, Elasticsearch, migrations, provider integrations
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

`/search` runs hybrid retrieval: Elasticsearch keyword/BM25 search plus Elasticsearch vector search over persisted chunk embeddings, followed by a simple normalized-score merge. `/ask` uses the same hybrid retrieval path, builds a grounded prompt, calls the configured chat model, and returns citations. Scanned document citations can include page and section metadata.

## Prerequisites

- .NET 10 SDK
- EF Core CLI tool matching the EF Core package major/minor used by the project
- Ollama running locally for Phase 1 embeddings and chat
- Elasticsearch running locally for Phase 4 retrieval
- Azure AI Document Intelligence resource for scanned PDF/image ingestion

Recommended EF tool setup:

```bash
dotnet tool update --global dotnet-ef --version 10.0.7
```

Recommended Ollama setup:

```bash
ollama pull nomic-embed-text
ollama pull llama3.2
ollama list
```

Recommended Elasticsearch setup:

```bash
docker network create elastic
docker run --name es01 \
  --net elastic \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  -e "xpack.security.http.ssl.enabled=false" \
  -e "xpack.security.transport.ssl.enabled=false" \
  -p 9200:9200 \
  -m 1GB \
  docker.elastic.co/elasticsearch/elasticsearch:9.3.4
```

Kibana and cleanup commands are documented in `docs/ELASTIC_KIBANA.md`.

## Local Development

Restore and build:

```bash
dotnet restore
dotnet build ProjectRag.slnx
```

Run tests:

```bash
dotnet test ProjectRag.Tests/ProjectRag.Tests.csproj
```

Apply local SQLite migrations:

```bash
dotnet ef database update \
  --project ./ProjectRag.Infrastructure/ProjectRag.Infrastructure.csproj \
  --startup-project ./ProjectRag.Api/ProjectRag.Api.csproj
```

Run the API:

```bash
dotnet run --project ProjectRag.Api
```

OpenAPI is mapped in development with `MapOpenApi()`.

Ingest the sample corpus with `ProjectRag.Api/ProjectRag.Api.http` or an HTTP client. Some `.http` runners have short request timeouts, so curl is more reliable while ingestion still runs inline:

```bash
curl -X POST "http://localhost:5260/api/v1/ingestions" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  --max-time 300 \
  -d '{"sourcePath":"samples/docs"}'
```

Then search:

```json
{
  "query": "charges for paying bills after the due date",
  "topK": 5,
  "filters": {
    "sourceType": "md"
  }
}
```

Then ask:

```json
{
  "question": "What are the late payment fees?",
  "topK": 5,
  "filters": {
    "sourceType": "md"
  }
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

Local development uses SQLite for EF Core metadata:

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

Elasticsearch stores the active search index:

```json
"Elasticsearch": {
  "Endpoint": "http://localhost:9200",
  "IndexName": "projectrag-chunks",
  "TimeoutSeconds": 120
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

```bash
dotnet user-secrets init --project ./ProjectRag.Api/ProjectRag.Api.csproj
dotnet user-secrets set "DocumentIntelligence:Endpoint" "https://YOUR-RESOURCE.cognitiveservices.azure.com/" --project ./ProjectRag.Api/ProjectRag.Api.csproj
dotnet user-secrets set "DocumentIntelligence:ApiKey" "YOUR-KEY" --project ./ProjectRag.Api/ProjectRag.Api.csproj
```

The SQLite database file is local runtime state and should not be committed. Secrets should not be stored in committed `appsettings` files. Use user-secrets, environment variables, or deployment secret stores for credentials.

## Hybrid Retrieval

Phase 4 stores searchable chunk records in Elasticsearch during ingestion. Each record includes chunk text, metadata, and an embedding generated with Ollama.

```text
ingestion: document -> chunks -> chunk embeddings -> Elasticsearch index
search: query -> keyword search + vector search -> merge candidates -> ranked hits
```

Content hashing prevents unchanged source files from being re-extracted and re-chunked. If a file changes, the ingestion flow deletes old search records for the document, replaces its chunks, and indexes the new chunks.

The Phase 4 merge is intentionally simple: vector and keyword scores are normalized independently, deduplicated by chunk id, and boosted when both retrieval paths match. Phase 6 will replace this with Reciprocal Rank Fusion.

## References

- ASP.NET Core Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis
- ASP.NET Core OpenAPI: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi
- EF Core SQLite provider: https://learn.microsoft.com/en-us/ef/core/providers/sqlite/
- EF Core migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- Microsoft.Extensions.AI local AI quickstart with Ollama: https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/quickstart-local-ai
- .NET vector stores: https://learn.microsoft.com/en-us/dotnet/ai/vector-stores/how-to/use-vector-stores
- .NET vector store ingestion: https://learn.microsoft.com/en-us/dotnet/ai/vector-stores/how-to/vector-store-data-ingestion
- Elastic .NET client: https://www.elastic.co/docs/reference/elasticsearch/clients/dotnet/installation
- Elasticsearch RRF retriever: https://www.elastic.co/docs/reference/elasticsearch/rest-apis/retrievers/rrf-retriever
- Ollama embeddings: https://docs.ollama.com/capabilities/embeddings
- Azure AI Document Intelligence layout model: https://learn.microsoft.com/azure/ai-services/document-intelligence/prebuilt/layout
- ASP.NET Core app secrets: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
