using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Triage.Application.Redaction;

namespace Triage.Infrastructure.Redaction;

/// <summary>Deterministic regex/NER pass via Microsoft Presidio's /analyze endpoint. Primary detector.</summary>
public sealed class PresidioPiiDetector : IPiiDetector
{
    public string Name => "presidio";

    private readonly HttpClient _httpClient;
    private readonly ILogger<PresidioPiiDetector> _logger;

    public PresidioPiiDetector(HttpClient httpClient, ILogger<PresidioPiiDetector> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<(int Start, int Length, string EntityType)>> DetectAsync(
        string fieldName, string text, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/analyze", new PresidioAnalyzeRequest(text, "en"), ct);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<PresidioAnalyzeResult>>(cancellationToken: ct)
                ?? new List<PresidioAnalyzeResult>();

            return results
                .Select(r => (r.Start, r.End - r.Start, r.EntityType))
                .ToList();
        }
        catch (Exception ex)
        {
            // A detector outage should not block triage entirely — the Ollama secondary pass
            // and whatever spans it does catch are still better than failing the whole ticket.
            _logger.LogError(ex, "Presidio analyze call failed for field {Field}; continuing without its spans.", fieldName);
            return Array.Empty<(int, int, string)>();
        }
    }

    private sealed record PresidioAnalyzeRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("language")] string Language);

    private sealed record PresidioAnalyzeResult(
        [property: JsonPropertyName("entity_type")] string EntityType,
        [property: JsonPropertyName("start")] int Start,
        [property: JsonPropertyName("end")] int End,
        [property: JsonPropertyName("score")] double Score);
}
