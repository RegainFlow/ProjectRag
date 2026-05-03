# Future Notes

This file tracks known improvement ideas that are useful later, but should not distract from the current learning phase.

## Ingestion

- Move ingestion out of the request path.
  - Current `/ingestions` work runs inline and can exceed client timeouts.
  - Use the existing worker project or a background queue when the project reaches hardening.
- Add bulk indexing for Elasticsearch.
  - Current indexing is chunk-by-chunk.
  - Bulk indexing should reduce ingestion latency and partial-failure noise.
- Add ingestion observability.
  - Track document count, chunk count, extraction time, embedding time, indexing time, and failures.
- Add dead-letter handling.
  - Failed files should be recorded and retryable without rerunning the whole source folder.
- Keep content hashing.
  - Existing idempotency behavior is important; preserve it when ingestion moves async.

## Search

- Add timing diagnostics.
  - Split latency into query rewrite, embedding, vector search, keyword search, and merge.
- Add a rewrite toggle.
  - Query rewriting improves understanding, but local LLM rewrite can add noticeable latency.
  - Consider `enableQueryRewrite` or a config flag after the learning phase.
- Cache rewritten queries.
  - Same original query should not always require another LLM rewrite.
- Replace simple score merge with RRF.
  - Current hybrid merge is intentionally simple.
  - RRF should make fusion more stable across keyword and vector score scales.
- Evaluate search quality before optimizing.
  - Compare vector-only, keyword-only, hybrid, rewrite+hybrid, and later reranked results.
- Keep metadata filters provider-neutral at the API boundary.
  - Elasticsearch query details should stay inside Infrastructure.

## Ask

- Add timing diagnostics.
  - Split latency into rewrite, retrieval, prompt construction, and chat generation.
- Consider streaming answers.
  - `/ask` currently waits for the full chat response.
  - Streaming would improve perceived latency.
- Improve citation behavior.
  - Current citations are chunk-level.
  - Later phases should attach citations to specific claims.
- Strengthen refusal behavior.
  - Keep improving "not enough evidence" handling as the prompt and eval set mature.
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

- Build the eval harness before major provider/chunking experiments.
  - It will make model, retrieval, and chunking changes measurable instead of subjective.
- Track at minimum:
  - retrieval hit rate
  - expected-source match
  - citation correctness
  - groundedness
  - latency by stage
  - model/provider used
- Keep a small hand-written golden set first.
  - Start with sample docs, then add scanned-document cases.

## References

- Foundry Local documentation: https://learn.microsoft.com/en-us/azure/foundry-local/
- Foundry Local quickstart: https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/get-started
- Microsoft Foundry RAG evaluators: https://learn.microsoft.com/en-us/azure/foundry/concepts/evaluation-evaluators/rag-evaluators
- .NET vector stores: https://learn.microsoft.com/en-us/dotnet/ai/vector-stores/how-to/use-vector-stores
