using ProjectRag.Contracts;
using ProjectRag.Tests.Support;
using System.Net;
using System.Net.Http.Json;

namespace ProjectRag.Tests.Api;

public class AskEndpointsTests : IClassFixture<RagApiFactory>
{
    private readonly HttpClient _client;

    public AskEndpointsTests(RagApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ask_returns_answer_with_citations_for_ingested_documents()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-ask-test-");

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

            var askResponse = await _client.PostAsJsonAsync(
                "/api/v1/ask",
                new AskRequest("What are the late payment fees?", TopK: 5));

            Assert.Equal(HttpStatusCode.OK, askResponse.StatusCode);

            var body = await askResponse.Content.ReadFromJsonAsync<AskResponse>();

            Assert.NotNull(body);
            Assert.Contains("monthly fee", body.Answer);
            Assert.NotEmpty(body.Citations);

            var firstCitation = body.Citations[0];

            Assert.False(string.IsNullOrWhiteSpace(firstCitation.DocumentId));
            Assert.False(string.IsNullOrWhiteSpace(firstCitation.ChunkId));
            Assert.Equal(filePath, firstCitation.SourceUri);
            Assert.True(firstCitation.Score > 0);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
