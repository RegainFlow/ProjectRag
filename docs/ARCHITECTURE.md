# Architecture

ProjectRag is a layered .NET RAG service. The architecture is intentionally conservative: establish clear boundaries, persistence, API contracts, testability, scanned document extraction, persistent search indexing, hybrid retrieval, query rewriting, RRF fusion, and semantic reranking before introducing grounded-answer hardening or agentic orchestration.

## Layers

```text
ProjectRag.Api
  Minimal API endpoints
  Composition root
  OpenAPI setup

ProjectRag.Contracts
  Request and response DTOs
  HTTP boundary models

ProjectRag.Domain
  Persistent domain entities
  Domain enums

ProjectRag.Infrastructure
  EF Core DbContext
  SQLite provider registration
  Elasticsearch client and search index services
  Entity configurations
  Migrations
  Text ingestion
  Azure AI Document Intelligence extraction
  Layout-aware chunk normalization
  Ollama AI client registration
  LLM query rewriting
  Reciprocal Rank Fusion
  Local LLM reranking
  Hybrid keyword/vector retrieval

ProjectRag.Application
  Application abstractions and cross-layer models

ProjectRag.Ingestion.Worker
  Future background ingestion processing

ProjectRag.Tests
  Integration and unit tests
```

## Dependency Direction

The current dependency shape is:

```text
Api -> Contracts
Api -> Infrastructure
Infrastructure -> Domain
Infrastructure -> Application
Application -> Domain
Tests -> Api, Contracts, Infrastructure
```

The domain project should stay independent. It should not reference EF Core, ASP.NET Core, Infrastructure, or API.

## Type Visibility

Keep the public surface area small:

- Infrastructure concrete services/configurations/options: `internal`.
- Infrastructure DI entry point and `RagDbContext`: `public`.
- Application abstractions/models: `public`.
- Domain entities/enums: `public`.
- Contract request/response DTOs: `public`.
- API endpoint mapping classes: `internal`.
- `Program`: `public partial` so integration tests can use `WebApplicationFactory<Program>`.

```mermaid
flowchart LR
    subgraph Hosts["Hosts"]
        Api["ProjectRag.Api"]
        Worker["ProjectRag.Ingestion.Worker"]
    end

    subgraph Boundary["HTTP Contracts"]
        Contracts["ProjectRag.Contracts"]
    end

    subgraph Core["Core"]
        Application["ProjectRag.Application"]
        Domain["ProjectRag.Domain"]
    end

    subgraph Data["Data Access"]
        Infrastructure["ProjectRag.Infrastructure"]
    end

    subgraph TestLayer["Tests"]
        Tests["ProjectRag.Tests"]
    end

    Api --> Contracts
    Api --> Infrastructure
    Api --> Application
    Api --> Domain

    Worker --> Contracts
    Worker --> Infrastructure
    Worker --> Application

    Infrastructure --> Application
    Infrastructure --> Domain
    Application --> Domain

    Tests -. verifies .-> Api
    Tests -. verifies .-> Contracts
    Tests -. verifies .-> Infrastructure
```

## Persistence Model

The persistence layer stores EF Core metadata plus external search indexes:

- `Document`: one original source document.
- `DocumentChunk`: one searchable text chunk belonging to a document.
- `IngestionJob`: status record for document ingestion work.
- `projectrag-chunks`: Elasticsearch index keyed by chunk id.

Current EF Core tables:

```text
Documents
DocumentChunks
IngestionJobs
```

`DocumentChunk` has a required relationship to `Document` and cascades on document deletion. `IngestionJob` is independent for now. Elasticsearch stores chunk text, metadata, and embeddings outside EF Core migrations.

```mermaid
erDiagram
    DOCUMENTS ||--o{ DOCUMENT_CHUNKS : contains

    DOCUMENTS {
        guid Id
        string SourceUri
        string Title
        string ContentHash
        string SourceType
        datetime CreatedAt
        datetime UpdatedAt
    }

    DOCUMENT_CHUNKS {
        guid Id
        guid DocumentId
        int ChunkIndex
        string Text
        int PageNumber
        string SectionTitle
        string LayoutRole
        string BoundingRegionsJson
        int Kind
        datetime CreatedAt
    }

    INGESTION_JOBS {
        guid Id
        string SourcePath
        int Status
        string ErrorMessage
        datetime CreatedAt
        datetime StartedAt
        datetime CompletedAt
    }
```

## EF Core Configuration

EF mapping is configured with Fluent API classes in Infrastructure:

```text
ProjectRag.Infrastructure/Configurations/Persistence
```

`RagDbContext` applies these configurations through:

```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(RagDbContext).Assembly);
```

This keeps persistence mapping out of domain entities.

## RAG Flow

Implemented behavior:

- `POST /api/v1/ingestions` ingests `.md`, `.txt`, PDF, and common image files from a local path.
- `GET /api/v1/ingestions/{id}` returns a persisted ingestion job.
- `GET /api/v1/documents` reads documents from SQLite.
- `POST /api/v1/search` rewrites the query, runs Elasticsearch keyword search and Elasticsearch vector search, fuses candidates with RRF, reranks the fused candidates with a local LLM, and returns ranked hits with retrieval diagnostics.
- `POST /api/v1/ask` rewrites the question, retrieves top chunks, builds a grounded prompt, calls the chat model, and returns an answer with citations.

Chunk embeddings are generated once during ingestion and stored in Elasticsearch with chunk text and metadata. Search rewrites the original user query into a semantic query for vector retrieval and a keyword query for full-text retrieval. It embeds only the semantic query, runs keyword and vector retrieval independently, deduplicates candidates by chunk id, fuses the ranked lists with Reciprocal Rank Fusion, and reranks the fused candidate set. `RrfScore` is the fusion score, `RerankScore` is the second-stage relevance score, and `VectorScore` plus `KeywordScore` are raw provider scores for diagnostics.

Text and markdown files use paragraph-based fixed-size chunking. Scanned documents use Azure AI Document Intelligence `prebuilt-layout`, then a layout-aware rule-based chunking strategy:

- Sort extracted layout blocks by document span.
- Use headings as section boundaries.
- Keep tables as separate Markdown table chunks.
- Merge nearby paragraph fragments under the current heading.
- Preserve page number, section title, layout role, and bounding regions on chunks.

```mermaid
sequenceDiagram
    participant Client
    participant API as ProjectRag.Api
    participant Ingest as Document Ingestion
    participant Extract as Azure Document Intelligence
    participant Normalize as Layout Normalizer
    participant EF as RagDbContext
    participant DB as SQLite
    participant ES as Elasticsearch
    participant Rewrite as Query Rewrite
    participant Embed as Ollama Embeddings
    participant RRF as RRF Fusion
    participant Rerank as LLM Reranker
    participant Chat as Ollama Chat

    Client->>API: POST /api/v1/ingestions
    API->>Ingest: Read text/scanned files
    alt Scanned PDF/image
        Ingest->>Extract: Analyze with prebuilt-layout
        Extract->>Normalize: Paragraphs, tables, pages, bounding regions
        Normalize-->>Ingest: Layout-aware chunks
    else Text/Markdown
        Ingest->>Ingest: Paragraph-based chunking
    end
    Ingest->>EF: Add Documents and DocumentChunks
    EF->>DB: INSERT Documents, DocumentChunks, IngestionJobs
    Ingest->>Embed: Embed chunk text
    Ingest->>ES: Upsert chunk text, metadata, and vectors
    API-->>Client: 202 Accepted + completed IngestionJobResponse

    Client->>API: POST /api/v1/search
    API->>Rewrite: Rewrite original query
    Rewrite-->>API: Semantic query + keyword query
    API->>Embed: Embed semantic query
    API->>ES: Vector search
    API->>ES: Keyword search
    API->>RRF: Fuse ranked lists
    API->>Rerank: Rerank fused candidates
    API-->>Client: Ranked SearchResponse

    Client->>API: POST /api/v1/ask
    API->>Rewrite: Rewrite question
    Rewrite-->>API: Semantic query + keyword query
    API->>Embed: Embed semantic query
    API->>ES: Hybrid retrieval
    API->>RRF: Fuse ranked lists
    API->>Rerank: Rerank fused candidates
    API->>Chat: Grounded prompt with top chunks
    Chat-->>API: Answer text
    API-->>Client: AskResponse with citations
```

## Testing Strategy

Current integration tests use:

- `WebApplicationFactory<Program>`
- SQLite in-memory database
- DI replacement of `RagDbContext`
- fake embedding generator
- fake chat client
- fake document extractor
- fake keyword/vector retrieval services for API tests
- fake query rewrite service for API tests
- fake reranker service for API tests
- direct tests for RRF rank fusion
- direct tests for LLM reranking fallback and scoring
- direct tests for layout block normalization

This verifies API + DI + EF Core + extraction/ingestion + retrieval/answer behavior without mutating the developer's local SQLite file and without requiring Ollama, Azure, or Elasticsearch during normal tests. Elasticsearch behavior is currently covered by manual local smoke testing.

## Current Limitations

- Ingestion runs inline in the API request.
- Long-running `/ingestions` and `/ask` calls can exceed short `.http` client timeouts; use curl with a longer timeout while ingestion remains inline.
- Chunking is paragraph/layout based, not semantic, recursive, token based, or overlapping.
- Query rewriting and LLM reranking use a local chat model and can add noticeable latency.
- Elasticsearch integration is manually smoke-tested, not part of the default automated test suite.
- Changed-file reingestion has a skipped regression test pending a focused EF tracking design pass.
- `/ask` is grounded by prompt instruction and citations, but claim-level citation validation is not implemented.
- The current reranker is educational and prompt-driven. Provider-native reranking, structured output enforcement, and local ONNX cross-encoders are future improvements.
- Agentic behavior is a later phase.
