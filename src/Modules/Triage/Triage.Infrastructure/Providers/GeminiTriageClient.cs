using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Triage.Application;
using Triage.Application.Providers;

namespace Triage.Infrastructure.Providers;

public sealed class GeminiTriageClient : ITriageLlmClient
{
    public string ProviderKey => "gemini";

    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _apiKey;

    public GeminiTriageClient(HttpClient httpClient, string apiKey, string model)
    {
        _httpClient = httpClient;
        _model = model;
        _apiKey = apiKey;
    }

    public async Task<TriageResult> TriageAsync(TicketContent maskedTicket, CancellationToken ct)
    {
        var request = new GenerateContentRequest(
            new[] { new Content(new[] { new Part(TriagePrompt.Build(maskedTicket)) }) });

        var response = await _httpClient.PostAsJsonAsync(
            $"/v1beta/models/{_model}:generateContent?key={_apiKey}", request, ct);
        response.EnsureSuccessStatusCode();

        var generated = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(cancellationToken: ct);
        var text = generated?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Gemini returned no candidate content.");

        return TriagePrompt.Parse(text);
    }

    private sealed record GenerateContentRequest([property: JsonPropertyName("contents")] Content[] Contents);
    private sealed record Content([property: JsonPropertyName("parts")] Part[] Parts);
    private sealed record Part([property: JsonPropertyName("text")] string Text);

    private sealed record GenerateContentResponse(
        [property: JsonPropertyName("candidates")] List<Candidate>? Candidates);

    private sealed record Candidate([property: JsonPropertyName("content")] Content? Content);
}
