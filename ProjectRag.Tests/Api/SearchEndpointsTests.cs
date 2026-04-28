using ProjectRag.Contracts;
using ProjectRag.Tests.Support;
using System.Net;
using System.Net.Http.Json;

namespace ProjectRag.Tests.Api;

public sealed class SearchEndpointsTests : IClassFixture<RagApiFactory>
{
    private readonly HttpClient _client;

    public SearchEndpointsTests(RagApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_returns_ranked_hits_for_ingested_documents()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-search-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "late-payment-policy.md");

            await File.WriteAllTextAsync(filePath, """
                # Late Payment Policy

                Late balances may receive a monthly fee after a grace period.
                """);

            var ingestionResponse = await _client.PostAsJsonAsync(
                "/api/v1/ingestions",
                new StartIngestionRequest(filePath));

            Assert.Equal(HttpStatusCode.Accepted, ingestionResponse.StatusCode);

            var searchResponse = await _client.PostAsJsonAsync(
                "/api/v1/search",
                new SearchRequest("late payment fees", TopK: 5));

            Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);

            var body = await searchResponse.Content.ReadFromJsonAsync<SearchResponse>();

            Assert.NotNull(body);
            Assert.Equal("late payment fees", body.Query);
            Assert.NotEmpty(body.Results);

            var first = body.Results[0];

            Assert.False(string.IsNullOrWhiteSpace(first.DocumentId));
            Assert.False(string.IsNullOrWhiteSpace(first.ChunkId));
            Assert.Equal(filePath, first.SourceUri);
            Assert.Contains("monthly fee", first.TextPreview);
            Assert.True(first.Score > 0);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
