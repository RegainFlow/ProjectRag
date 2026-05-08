namespace ProjectRag.Api.Options;

internal sealed class OpenTelemetryOptions
{
    public const string SectionName = "OpenTelemetry";
    public string ServiceName { get; set; } = "ProjectRag.Api";
    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
    public bool EnableConsoleExporter { get; set; } = false;
}
