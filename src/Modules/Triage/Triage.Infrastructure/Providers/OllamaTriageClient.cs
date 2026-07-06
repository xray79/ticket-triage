using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Triage.Application;
using Triage.Application.Providers;

namespace Triage.Infrastructure.Providers;

/// <summary>The floor of the fallback chain — always available, no further fallback beneath it.</summary>
public sealed class OllamaTriageClient : ITriageLlmClient
{
    public string ProviderKey => "local";

    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OllamaTriageClient(HttpClient httpClient, string model)
    {
        _httpClient = httpClient;
        _model = model;
    }

    public async Task<TriageResult> TriageAsync(TicketContent maskedTicket, CancellationToken ct)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/generate", new OllamaGenerateRequest(
            _model, TriagePrompt.Build(maskedTicket), false, "json"), ct);
        response.EnsureSuccessStatusCode();

        var generated = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        return TriagePrompt.Parse(generated.Response);
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string Response);
}
