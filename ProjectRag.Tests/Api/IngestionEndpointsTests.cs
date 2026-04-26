using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectRag.Contracts;
using ProjectRag.Infrastructure;
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
    public async Task PostIngestion_creates_pending_ingestion_job()
    {
        var request = new StartIngestionRequest("samples/invoice-001.pdf");

        var response = await _client.PostAsJsonAsync("/api/v1/ingestions", request);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<IngestionJobResponse>();

        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body.IngestionId);
        Assert.Equal(request.SourcePath, body.SourcePath);
        Assert.Equal("Pending", body.Status);
    }

    [Fact]
    public async Task GetIngestion_returns_created_job()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/ingestions",
            new StartIngestionRequest("samples/contract-001.pdf"));

        var created = await createResponse.Content.ReadFromJsonAsync<IngestionJobResponse>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/v1/ingestions/{created.IngestionId}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<IngestionJobResponse>();

        Assert.NotNull(fetched);
        Assert.Equal(created.IngestionId, fetched.IngestionId);
        Assert.Equal("samples/contract-001.pdf", fetched.SourcePath);
        Assert.Equal("Pending", fetched.Status);
    }
}

public sealed class RagApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RagDbContext>>();

            services.AddDbContext<RagDbContext>(options =>
                options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}