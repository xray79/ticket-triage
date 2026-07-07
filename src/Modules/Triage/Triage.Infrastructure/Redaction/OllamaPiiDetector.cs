using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Triage.Application.Redaction;

namespace Triage.Infrastructure.Redaction;

/// <summary>
/// Secondary, supplementary PII pass over free text using a local Ollama model — catches
/// indirect/contextual mentions ("my daughter Sarah's account") that Presidio's regex/NER
/// patterns don't cover. Asked to return the literal substrings rather than offsets, because
/// LLMs are unreliable at character-accurate positions; we locate each substring ourselves.
/// </summary>
public sealed class OllamaPiiDetector : IPiiDetector
{
    public string Name => "ollama-secondary";

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OllamaPiiDetector> _logger;

    private const string Instructions = """
        Find any personally identifiable information (names, emails, phone numbers,
        addresses, account numbers, IP addresses) mentioned in the text below, including
        indirect references (e.g. "my daughter Sarah"). Respond with ONLY a JSON array of
        objects: [{"text": "<exact substring>", "type": "<PERSON|EMAIL|PHONE|ADDRESS|ACCOUNT|IP|OTHER>"}].
        If none, respond with [].
        """;

    public OllamaPiiDetector(HttpClient httpClient, ILogger<OllamaPiiDetector> logger, string model)
    {
        _httpClient = httpClient;
        _logger = logger;
        _model = model;
    }

    public async Task<IReadOnlyList<(int Start, int Length, string EntityType)>> DetectAsync(
        string fieldName, string text, CancellationToken ct)
    {
        try
        {
            var prompt = $"{Instructions}\n\nText: {text}";

            var response = await _httpClient.PostAsJsonAsync("/api/generate", new OllamaGenerateRequest(
                _model, prompt, false), ct);
            response.EnsureSuccessStatusCode();

            var generated = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
            if (generated?.Response is null)
                return Array.Empty<(int, int, string)>();

            var matches = ParseMatches(generated.Response);
            var spans = new List<(int Start, int Length, string EntityType)>();

            foreach (var match in matches)
            {
                if (string.IsNullOrEmpty(match.Text))
                    continue;

                var index = 0;
                while ((index = text.IndexOf(match.Text, index, StringComparison.Ordinal)) >= 0)
                {
                    spans.Add((index, match.Text.Length, match.Type ?? "OTHER"));
                    index += match.Text.Length;
                }
            }

            return spans;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ollama secondary redaction pass failed for field {Field}; continuing with Presidio-only spans.", fieldName);
            return Array.Empty<(int, int, string)>();
        }
    }

    private static List<OllamaPiiMatch> ParseMatches(string modelResponse)
    {
        var jsonStart = modelResponse.IndexOf('[');
        var jsonEnd = modelResponse.LastIndexOf(']');
        if (jsonStart < 0 || jsonEnd < jsonStart)
            return new List<OllamaPiiMatch>();

        var jsonSlice = modelResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);

        try
        {
            return JsonSerializer.Deserialize<List<OllamaPiiMatch>>(jsonSlice) ?? new List<OllamaPiiMatch>();
        }
        catch (JsonException)
        {
            return new List<OllamaPiiMatch>();
        }
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);

    private sealed record OllamaPiiMatch(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("type")] string? Type);
}
