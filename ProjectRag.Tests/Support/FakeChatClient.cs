using Microsoft.Extensions.AI;

namespace ProjectRag.Tests.Support;

public sealed class FakeChatClient : IChatClient
{
    private readonly string _responseText;
    public FakeChatClient()
        : this("Late balances may receive a monthly fee after a grace period.")
    {
    }

    public FakeChatClient(string responseText)
    {
        _responseText = responseText;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responseText)));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return AsyncEnumerable.Empty<ChatResponseUpdate>();
    }

    public void Dispose()
    {
    }
}
