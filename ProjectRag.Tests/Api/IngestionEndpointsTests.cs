using ProjectRag.Contracts;
using ProjectRag.Tests.Support;
using System.Net;
using System.Net.Http.Json;

namespace ProjectRag.Tests.Api;

public sealed class IngestionEndpointsTests : IClassFixture<RagApiFactory>
{
    private readonly HttpClient _client;
    public IngestionEndpointsTests(RagApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostIngestion_ingests_markdown_file_and_completes_job()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-api-ingestion-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "late-payment-policy.md");

            await File.WriteAllTextAsync(filePath, """
                # Late Payment Policy

                Invoices are due 30 calendar days after the invoice date.
                """);

            var request = new StartIngestionRequest(filePath);

            var response = await _client.PostAsJsonAsync("/api/v1/ingestions", request);

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

            var body = await response.Content.ReadFromJsonAsync<IngestionJobResponse>();

            Assert.NotNull(body);

            Assert.NotEqual(Guid.Empty, body.IngestionId);
            Assert.Equal(filePath, body.SourcePath);

            Assert.Equal("Completed", body.Status);
            Assert.NotNull(body.StartedAt);
            Assert.NotNull(body.CompletedAt);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GetIngestion_returns_created_job()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-api-ingestion-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "invoice-disputes.md");

            await File.WriteAllTextAsync(filePath, """
                # Invoice Disputes

                Customers must submit invoice disputes within 15 calendar days.
                """);

            var createResponse = await _client.PostAsJsonAsync(
                "/api/v1/ingestions",
                new StartIngestionRequest(filePath));

            var created = await createResponse.Content.ReadFromJsonAsync<IngestionJobResponse>();

            Assert.NotNull(created);

            var getResponse = await _client.GetAsync($"/api/v1/ingestions/{created.IngestionId}");

            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var fetched = await getResponse.Content.ReadFromJsonAsync<IngestionJobResponse>();

            Assert.NotNull(fetched);

            Assert.Equal(created.IngestionId, fetched.IngestionId);
            Assert.Equal(filePath, fetched.SourcePath);

            Assert.Equal("Completed", fetched.Status);
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task GetDocuments_returns_ingested_documents()
    {
        var tempDirectory = Directory.CreateTempSubdirectory("projectrag-api-ingestion-test-");

        try
        {
            var filePath = Path.Combine(tempDirectory.FullName, "refund-policy.md");

            await File.WriteAllTextAsync(filePath, """
                # Refund Policy

                Refund requests must be submitted within 30 days.
                """);

            await _client.PostAsJsonAsync("/api/v1/ingestions", new StartIngestionRequest(filePath));

            var documents = await _client.GetFromJsonAsync<IReadOnlyList<DocumentSummaryResponse>>("/api/v1/documents");

            Assert.NotNull(documents);
            Assert.Contains(documents, x => x.Title == "refund-policy");
        }
        finally
        {
            tempDirectory.Delete(recursive: true);
        }
    }
}
