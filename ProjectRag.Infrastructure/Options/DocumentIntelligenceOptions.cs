namespace ProjectRag.Infrastructure.Options;

internal sealed class DocumentIntelligenceOptions
{
    public const string SectionName = "DocumentIntelligence";

    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ModelId { get; set; } = "prebuilt-layout";
}
