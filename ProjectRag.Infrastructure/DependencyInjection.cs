using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using ProjectRag.Application.Abstractions;
using ProjectRag.Infrastructure.AI;
using ProjectRag.Infrastructure.Ingestion;
using ProjectRag.Infrastructure.Options;
using ProjectRag.Infrastructure.VectorSearch;

namespace ProjectRag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddAiProviders(configuration);
        services.AddIngestion(configuration);
        services.AddVectorSearch(configuration);
        services.AddAiGeneration(configuration);

        return services;
    }

    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProjectRagDb")
            ?? throw new InvalidOperationException("Connection string 'ProjectRagDb' was not found.");

        services.AddDbContext<RagDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }

    public static IServiceCollection AddAiProviders(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiOptions>(
            configuration.GetSection(AiOptions.SectionName));

        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.OllamaEndpoint),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };

            return new OllamaApiClient(
                httpClient,
                options.EmbeddingModel);
        });

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AiOptions>>().Value;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.OllamaEndpoint),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };

            return new OllamaApiClient(
                httpClient,
                options.ChatModel);
        });

        return services;
    }

    public static IServiceCollection AddIngestion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITextChunker, SimpleTextChunker>();
        services.AddScoped<ITextDocumentIngestionService, FileSystemTextDocumentIngestionService>();

        return services;
    }

    public static IServiceCollection AddVectorSearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddScoped<IVectorSearchService, InMemoryVectorSearchService>();
    }

    public static IServiceCollection AddAiGeneration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddScoped<IRagAnswerService, RagAnswerService>();
    }
}
