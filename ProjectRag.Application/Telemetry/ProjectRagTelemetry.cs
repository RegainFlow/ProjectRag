using System.Diagnostics;

namespace ProjectRag.Application.Telemetry;

public static class ProjectRagTelemetry
{
    public const string ActivitySourceName = "ProjectRag";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}