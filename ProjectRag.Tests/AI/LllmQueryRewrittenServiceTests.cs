using Microsoft.Extensions.AI;
using ProjectRag.Infrastructure.AI;

namespace ProjectRag.Tests.AI;

public sealed class LllmQueryRewrittenServiceTests
{
    [Fact]
    public async Task RewriteAsync_returns_rewritten_queries_for_valid_json()
    {
        var service = new LlmQueryRewriteService(new StubChatClient("""
              {
                "semanticQuery": "late payment fees penalties overdue invoices",
                "keywordQuery": "\"late payment\" OR overdue OR penalty"
              }
              """));

        var result = await service.RewriteAsync(
            "What does it say about late payment fees?",
            CancellationToken.None);

        Assert.Equal("What does it say about late payment fees?", result.OriginalQuery);
        Assert.Equal("late payment fees penalties overdue invoices", result.SemanticQuery);
        Assert.Equal("\"late payment\" OR overdue OR penalty", result.KeywordQuery);
        Assert.Equal("rewritten", result.Status);
    }

    [Fact]
    public async Task RewriteAsync_falls_back_for_invalid_json()
    {
        var service = new LlmQueryRewriteService(new StubChatClient("not json"));

        var result = await service.RewriteAsync(
            "late fees",
            CancellationToken.None);

        Assert.Equal("late fees", result.OriginalQuery);
        Assert.Equal("late fees", result.SemanticQuery);
        Assert.Equal("late fees", result.KeywordQuery);
        Assert.Equal("fallback", result.Status);
    }

    [Fact]
    public async Task RewriteAsync_falls_back_for_blank_query()
    {
        var service = new LlmQueryRewriteService(new StubChatClient("""
              {
                "semanticQuery": "should not matter",
                "keywordQuery": "should not matter"
              }
              """));

        var result = await service.RewriteAsync(
            "",
            CancellationToken.None);

        Assert.Equal("", result.OriginalQuery);
        Assert.Equal("", result.SemanticQuery);
        Assert.Equal("", result.KeywordQuery);
        Assert.Equal("fallback", result.Status);
    }
    private sealed class StubChatClient : IChatClient
    {
        private readonly string _responseText;

        public StubChatClient(string responseText)
        {
            _responseText = responseText;
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, _responseText)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return AsyncEnumerable.Empty<ChatResponseUpdate>();
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}

