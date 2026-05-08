# Aspire Dashboard

ProjectRag uses OpenTelemetry for local observability. During development, the standalone Aspire Dashboard can receive OTLP traces, metrics, and logs without requiring a full Aspire AppHost.

## Start Dashboard

```bash
docker run --rm -it \
  -p 18888:18888 \
  -p 4317:18889 \
  -p 4318:18890 \
  -e ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true \
  --name aspire-dashboard \
  mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Open:

```text
http://localhost:18888
```

The dashboard receives telemetry on:

```text
http://localhost:4317  # OTLP/gRPC
http://localhost:4318  # OTLP/HTTP
```

## ProjectRag Configuration

Development config should point OTLP export to the dashboard:

```json
"OpenTelemetry": {
  "ServiceName": "ProjectRag.Api",
  "OtlpEndpoint": "http://localhost:4317",
  "EnableConsoleExporter": false
}
```

## Smoke Test

1. Start the dashboard.
2. Run the API.
3. Call `/api/v1/search` or `/api/v1/ask`.
4. Open the Traces page in the dashboard.
5. Look for:
   - ASP.NET Core request span.
   - `rag.search`.
   - `rag.ask`.
   - RAG pipeline spans listed below.
   - outgoing HTTP spans for model/search calls, where available.

## Custom RAG Spans

ProjectRag emits custom spans from the shared `ProjectRag` `ActivitySource`.

Search traces should look similar to:

```text
POST /api/v1/search
  rag.search
    rag.query_rewrite
    rag.retrieval.hybrid
      rag.search.vector
      rag.search.keyword
      rag.rank_fusion.rrf
      rag.rerank
```

Ask traces should look similar to:

```text
POST /api/v1/ask
  rag.ask
    rag.answer
      rag.query_rewrite
      rag.retrieval.hybrid
        rag.search.vector
        rag.search.keyword
        rag.rank_fusion.rrf
        rag.rerank
      rag.answer_generation
```

Ingestion traces should look similar to:

```text
POST /api/v1/ingestions
  rag.ingestion
    rag.ingestion.file
```

Common tags include:

- `rag.top_k`
- `rag.top_k.effective`
- `rag.query.length`
- `rag.question.length`
- `rag.filters.source_type`
- `rag.results.count`
- `rag.context.count`
- `rag.candidates.count`
- `rag.answer.status`
- `rag.rerank.fallback`
- `rag.files.count`
- `rag.chunks.count`

Do not add raw questions, prompts, source text, or file contents as custom span tags. Use lengths, counts, status values, provider names, and low-cardinality diagnostic fields instead.

## GenAI Telemetry

The Aspire Dashboard has a GenAI telemetry visualizer for AI operations such as chat completions and embeddings.

The Ollama chat and embedding clients are wrapped with `Microsoft.Extensions.AI` OpenTelemetry support. Sensitive prompt and response content should remain disabled by default unless explicitly needed for local debugging.

## Optional Message Content Capture

Message content capture is controlled by the application process that emits telemetry, not by the Aspire Dashboard container. Keep it disabled by default because it can record prompts, retrieved chunks, user questions, and model responses.

For local debugging with synthetic data, enable it in the same shell session that starts the API.

```bash
export OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true
dotnet run --project ProjectRag.Api
```

If you start the API from PowerShell instead of bash:

```powershell
$env:OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT = "true"
dotnet run --project ProjectRag.Api
```

This variable does not belong on the Aspire Dashboard container. The dashboard only receives telemetry; the API process creates the chat and embedding telemetry.

If the API is running in a container, set the environment variable on the API container:

```bash
docker run \
  -e OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true \
  your-projectrag-api-image
```

Do not set this in shared, production, or real-document environments unless you have a deliberate data-handling policy for captured prompts and responses.

## Cleanup

```bash
docker stop aspire-dashboard
```

## References

- Standalone Aspire Dashboard: https://aspire.dev/dashboard/standalone/
- Aspire GenAI telemetry visualization: https://aspire.dev/dashboard/explore/#genai-telemetry-visualization
- Aspire telemetry: https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry
