using Azure.AI.DocumentIntelligence;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using ProjectRag.Application.Abstractions;
using ProjectRag.Infrastructure.AI;
using ProjectRag.Infrastructure.DocumentIntelligence;
using ProjectRag.Infrastructure.Ingestion;
using ProjectRag.Infrastructure.Options;
using ProjectRag.Infrastructure.Search;

namespace ProjectRag.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddOllama(configuration);
        services.AddAzureDocumentIntelligence(configuration);
        services.AddElasticsearch(configuration);
        services.AddIngestion(configuration);
        services.AddScoped<IQueryRewriteService, LlmQueryRewriteService>();

        services.AddScoped<IRagAnswerService, RagAnswerService>();

        return services;
    }

    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ProjectRagDb")
            ?? throw new InvalidOperationException("Connection string 'ProjectRagDb' was not found.");

        services.AddDbContext<RagDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }

    private static IServiceCollection AddOllama(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));

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

    private static IServiceCollection AddAzureDocumentIntelligence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DocumentIntelligenceOptions>(configuration.GetSection(DocumentIntelligenceOptions.SectionName));
        services.AddSingleton<DocumentIntelligenceClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<DocumentIntelligenceOptions>>().Value;

            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException("Document Intelligence endpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException("Document Intelligence apikey is not configured.");
            }

            return new DocumentIntelligenceClient(
                new Uri(options.Endpoint),
                new Azure.AzureKeyCredential(options.ApiKey));
        });

        return services;
    }

    private static IServiceCollection AddIngestion(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ITextChunker, SimpleTextChunker>();
        services.AddSingleton<IDocumentExtractor, AzureDocumentIntelligenceExtractor>();
        services.AddScoped<ITextDocumentIngestionService, FileSystemDocumentIngestionService>();

        return services;
    }

    private static IServiceCollection AddElasticsearch(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));

        services.AddSingleton<ElasticsearchClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.Endpoint))
            {
                throw new InvalidOperationException("Elasticsearch endpoint is not configured.");
            }

            if (string.IsNullOrWhiteSpace(options.IndexName))
            {
                throw new InvalidOperationException("Elasticsearch index name is not configured.");
            }

            var settings = new ElasticsearchClientSettings(new Uri(options.Endpoint))
                .DefaultIndex(options.IndexName)
                .RequestTimeout(TimeSpan.FromSeconds(options.TimeoutSeconds));

            return new ElasticsearchClient(settings);
        });

        services.AddScoped<ISearchIndexService, ElasticSearchIndexService>();
        services.AddScoped<IKeywordSearchService, ElasticKeywordSearchService>();
        services.AddScoped<IVectorSearchService, ElasticVectorSearchService>();
        services.AddScoped<IRetrievalSearchService, HybridRetrievalSearchService>();

        return services;
    }
}

