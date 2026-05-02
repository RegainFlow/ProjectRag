namespace ProjectRag.Application.Models;

public sealed record SearchFilters(
    string? SourceType = null,
    string? SourceUriContains = null,
    DateTime? CreatedFrom = null,
    DateTime? CreatedTo = null,
    int? PageFrom = null,
    int? PageTo = null);