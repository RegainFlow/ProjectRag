# Future Notes

This file tracks known improvement ideas that are useful later, but should not distract from the current learning phase.

## Ingestion

- Move ingestion out of the request path.
  - Current `/ingestions` work runs inline and can exceed client timeouts.
  - Use the existing worker project or a background queue when the project reaches hardening.
- Add bulk indexing for Elasticsearch.
  - Current indexing is chunk-by-chunk.
  - Bulk indexing should reduce ingestion latency and partial-failure noise.
- Improve ingestion observability.
  - Current spans track ingestion and file-level work.
  - Later, split document count, chunk count, extraction time, embedding time, indexing time, and failures into clearer stage spans or metrics.
- Add dead-letter handling.
  - Failed files should be recorded and retryable without rerunning the whole source folder.
- Keep content hashing.
  - Existing idempotency behavior is important; preserve it when ingestion moves async.

## Search

- Add timing diagnostics.
  - Split latency into query rewrite, embedding, vector search, keyword search, RRF, and reranking.
- Add a rewrite toggle.
  - Query rewriting improves understanding, but local LLM rewrite can add noticeable latency.
  - Consider `enableQueryRewrite` or a config flag after the learning phase.
- Cache rewritten queries.
  - Same original query should not always require another LLM rewrite.
- Compare application-level RRF with provider-native fusion later.
  - Current RRF is implemented in .NET for learning and provider neutrality.
  - Elasticsearch native RRF can be evaluated later as a provider optimization.
- Improve reranking performance and reliability.
  - Current reranking uses the local chat model and can be slow.
  - Compare local LLM reranking with Elasticsearch native reranking, Azure AI Search semantic ranking, and local ONNX cross-encoders.
  - Evaluate structured outputs so rerank responses are less dependent on prompt-following.
- Evaluate search quality before optimizing.
  - Compare vector-only, keyword-only, hybrid, rewrite+hybrid, and reranked results.
- Keep metadata filters provider-neutral at the API boundary.
  - Elasticsearch query details should stay inside Infrastructure.

## Ask

- Add timing diagnostics.
  - Split latency into rewrite, retrieval, reranking, prompt construction, and chat generation.
- Consider streaming answers.
  - `/ask` currently waits for the full chat response.
  - Streaming would improve perceived latency.
- Improve citation behavior.
  - Current claims cite chunk ids.
  - Later phases should validate that each claim is actually supported by its cited chunk.
  - Consider inline citation markers in the answer text after structured claims stabilize.
- Strengthen refusal behavior.
  - Current unsupported answers return `insufficientContext`.
  - Keep improving unsupported-answer detection as the prompt and eval set mature.
- Evaluate structured output enforcement.
  - Current answer parsing depends on prompt-following JSON.
  - Compare plain prompting with `ChatOptions.ResponseFormat` and provider-specific structured output support.
- Add cost/token logging.
  - Useful when comparing local, Azure OpenAI, Foundry Local, and other providers.

## Model And Provider Experiments

- Add model/provider abstraction after retrieval quality is measurable.
  - Avoid adding too many provider switches before the eval harness exists.
  - Target swappable chat and embedding providers behind existing Application interfaces.
- Explore Microsoft Foundry Local.
  - Use it as a local/on-device model provider candidate for chat and possibly embeddings.
  - Compare against Ollama using the same eval harness.
- Keep provider-specific details in Infrastructure.
  - API contracts should not expose Ollama, Azure OpenAI, Foundry Local, or Elasticsearch-specific types.
- Track model metadata in responses.
  - Later `/ask` and `/search` diagnostics should show chat model, embedding model, provider, and rewrite model.

## Chunking Experiments

- Compare chunking strategies with the eval harness.
  - Current text chunking is paragraph-based.
  - Current scanned document chunking is layout-aware and rule-based.
- Experiment later with:
  - token-aware chunking
  - overlapping chunks
  - recursive splitting
  - semantic chunking
  - table-preserving scanned-document chunks
- Measure impact before changing defaults.
  - Chunking changes should be judged by retrieval hit rate, citation correctness, answer groundedness, and latency.

## Evaluation Harness

- Expand the eval harness before major provider/chunking experiments.
  - The initial deterministic harness exists; keep broadening it so model, retrieval, and chunking changes stay measurable instead of subjective.
- Track at minimum:
  - retrieval hit rate
  - expected-source match
  - citation correctness
  - groundedness
  - latency by stage
  - model/provider used
- Keep a small hand-written golden set first.
  - Start with sample docs, then add scanned-document cases.
- Add Microsoft.Extensions.AI evaluation as a second layer.
  - Keep deterministic source-hit tests as the default regression signal.
  - Add optional quality evals for groundedness, relevance, completeness, equivalence, and reporting.

## Observability

- Keep Aspire Dashboard as the default local OTLP viewer.
  - A full Aspire AppHost can be considered later if the project grows into multiple always-running services.
- Add OpenTelemetry metrics after traces stabilize.
  - Useful metrics include request latency, retrieval latency by stage, retrieved candidate counts, rerank fallback count, ingestion file count, and ingestion failure count.
- Keep sensitive telemetry disabled by default.
  - Prompt and response capture should remain local-only and deliberate.

## References

- Foundry Local documentation: https://learn.microsoft.com/en-us/azure/foundry-local/
- Foundry Local quickstart: https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/get-started
- Microsoft Foundry RAG evaluators: https://learn.microsoft.com/en-us/azure/foundry/concepts/evaluation-evaluators/rag-evaluators
- Microsoft.Extensions.AI evaluation libraries: https://learn.microsoft.com/en-us/dotnet/ai/evaluation/libraries
- Aspire Dashboard standalone: https://aspire.dev/dashboard/standalone/
- .NET vector stores: https://learn.microsoft.com/en-us/dotnet/ai/vector-stores/how-to/use-vector-stores
