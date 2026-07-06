using System.Text.Json;

namespace Triage.Eval;

public static class EvalSampleLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<EvalSample> LoadFromJson(string json) =>
        JsonSerializer.Deserialize<List<EvalSample>>(json, JsonOptions)
            ?? throw new InvalidOperationException("Eval sample file did not deserialize to a list.");

    public static IReadOnlyList<EvalSample> LoadFromFile(string path) => LoadFromJson(File.ReadAllText(path));
}
