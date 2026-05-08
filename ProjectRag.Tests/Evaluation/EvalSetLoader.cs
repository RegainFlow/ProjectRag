using System.Text.Json;

namespace ProjectRag.Tests.Evaluation;

internal static class EvalSetLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static IReadOnlyList<EvalCase> LoadEvalSet()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Evaluation", "evalset.json");

        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<IReadOnlyList<EvalCase>>(json, JsonOptions) ?? [];
    }
}
