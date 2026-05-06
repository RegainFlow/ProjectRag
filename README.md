# ProjectRag

ProjectRag is a learning-first .NET RAG service. The project is being built in phases so each layer introduces one production retrieval-augmented generation capability at a time.

Current status: Phase 8 grounded answer generation is implemented. The API can ingest local text/markdown files plus scanned PDFs/images with Azure AI Document Intelligence, persist layout-aware chunks in SQLite, index chunk text and embeddings into Elasticsearch, rewrite user questions into semantic and keyword search queries, fuse keyword/vector results with Reciprocal Rank Fusion, rerank fused candidates with a local LLM, and generate grounded answers with structured claims, citations, refusal status, retrieval diagnostics, and model info.

## Phase Roadmap

1. Phase 0: .NET Minimal API foundation, EF Core, SQLite, OpenAPI, clean layering.
2. Phase 1: Naive vector-only RAG over plain text or markdown documents.
3. Phase 2: Scanned PDF/image ingestion with Azure AI Document Intelligence.
4. Phase 3: Persistent local RAG with content-hash idempotency.
5. Phase 4: Elasticsearch hybrid keyword + vector retrieval with metadata filters.
6. Phase 5: LLM-powered query rewriting before retrieval.
7. Phase 6: Reciprocal Rank Fusion for keyword/vector result fusion.
8. Phase 7: Local LLM semantic reranking after RRF.
9. Phase 8: Stricter grounded answers with structured claims, refusal status, diagnostics, and model info.
10. Phase 9+: Evaluation harness, agentic RAG, and enterprise hardening.

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

`/search` rewrites the original query into semantic and keyword search queries, runs Elasticsearch vector search plus keyword/BM25 search, fuses candidates with Reciprocal Rank Fusion, then reranks the fused candidates with the configured local chat model. `/ask` uses the same rewritten hybrid retrieval path, builds a stricter grounded prompt, calls the configured chat model, and returns an answer status, structured claims, citations, retrieval diagnostics, and model info. Responses include query rewrite and retrieval diagnostics, including `rrfScore`, `rerankScore`, raw vector score, raw keyword score, and match source. Scanned document citations can include page and section metadata.

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

Then search. The response includes `queryRewrite` diagnostics:

```json
{
  "query": "what does it say about late payment fees?",
  "topK": 5,
  "filters": {
    "sourceType": "md"
  }
}
```

Then ask. The response includes the same rewrite diagnostics plus answer status, structured claims, citations, retrieval diagnostics, and model info:

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

## Query Rewriting, RRF, And Reranking

Phase 5 adds an LLM-powered rewrite step before retrieval. The rewrite service returns the original query, a semantic query for vector retrieval, and a keyword query for full-text retrieval. If rewriting fails, retrieval falls back to the original query.

Phase 6 fuses vector and keyword result lists with Reciprocal Rank Fusion. Phase 7 reranks the fused candidates with a local LLM. `RrfScore` remains the fusion score, `RerankScore` is the second-stage semantic relevance score, and `VectorScore` plus `KeywordScore` remain raw provider scores for diagnostics.

Searchable chunk records are stored in Elasticsearch during ingestion. Each record includes chunk text, metadata, and an embedding generated with Ollama.

```text
ingestion: document -> chunks -> chunk embeddings -> Elasticsearch index
search: original query -> query rewrite -> keyword search + vector search -> RRF fusion -> LLM reranking -> ranked hits
```

Content hashing prevents unchanged source files from being re-extracted and re-chunked. If a file changes, the ingestion flow deletes old search records for the document, replaces its chunks, and indexes the new chunks.

The current RRF formula is `score = sum(1 / (60 + rank))`, where rank starts at 1 in each result list. RRF is implemented in application code through `IRankFusionService` rather than Elasticsearch native RRF so the fusion behavior stays provider-neutral and visible for learning.

The current reranker is implemented through `IRerankerService` using the local `IChatClient`. This is useful for learning the broad-recall then precision-rerank pattern, but it is slower than a dedicated reranker model or provider-native reranking.

## Grounded Answers

Phase 8 makes `/ask` stricter and more auditable. The answer model is asked to return JSON with an `answerStatus`, answer text, and structured claims. Each claim references citation chunk ids through source indexes from the retrieved context. If retrieval returns no context or the model response cannot be parsed into cited claims, `/ask` returns `answerStatus: "insufficientContext"` instead of an uncited answer.

The `/ask` response includes:

- `answer`: final answer or refusal text.
- `answerStatus`: `answered` or `insufficientContext`.
- `claims`: claim text plus citation chunk ids.
- `citations`: chunk-level source metadata and retrieval scores.
- `retrievalDiagnostics`: requested top K, returned context count, and whether reranking was applied.
- `modelInfo`: chat provider, chat model, and embedding model.

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
- Microsoft.Extensions.AI ChatOptions response format: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.chatoptions.responseformat
- Ollama structured outputs: https://docs.ollama.com/capabilities/structured-outputs
- Ollama embeddings: https://docs.ollama.com/capabilities/embeddings
- Azure AI Document Intelligence layout model: https://learn.microsoft.com/azure/ai-services/document-intelligence/prebuilt/layout
- ASP.NET Core app secrets: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
