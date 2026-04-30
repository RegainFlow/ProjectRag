using Azure.AI.DocumentIntelligence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProjectRag.Application.Abstractions;
using ProjectRag.Infrastructure;
using ProjectRag.Infrastructure.VectorSearch;

namespace ProjectRag.Tests.Support;

public sealed class RagApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IVectorIndexService>();
            services.RemoveAll<IVectorSearchService>();
            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.RemoveAll<IChatClient>();
            services.RemoveAll<IDocumentExtractor>();
            services.RemoveAll<DocumentIntelligenceClient>();
            services.RemoveAll<DbContextOptions<RagDbContext>>();

            services.AddSingleton<IVectorIndexService, FakeVectorIndexService>();
            services.AddScoped<IVectorSearchService, InMemoryVectorSearchService>();
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>, FakeEmbeddingGenerator>();
            services.AddSingleton<IChatClient, FakeChatClient>();
            services.AddSingleton<IDocumentExtractor, FakeDocumentExtractor>();

            services.AddDbContext<RagDbContext>(options =>
                options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RagDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
