using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ProjectRag.Api.Options;
using ProjectRag.Application.Telemetry;

namespace ProjectRag.Api.Telemetry;

internal static class TelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddProjectRagOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>() ?? new OpenTelemetryOptions();

        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(ProjectRagTelemetry.ActivitySourceName);

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporter =>
                    {
                        exporter.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });

        return services;
    }
}