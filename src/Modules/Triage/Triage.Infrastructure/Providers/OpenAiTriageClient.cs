using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Triage.Application;
using Triage.Application.Providers;

namespace Triage.Infrastructure.Providers;

public sealed class OpenAiTriageClient : ITriageLlmClient
{
    public string ProviderKey => "openai";

    private readonly HttpClient _httpClient;
    private readonly string _model;

    public OpenAiTriageClient(HttpClient httpClient, string apiKey, string model)
    {
        _httpClient = httpClient;
        _model = model;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<TriageResult> TriageAsync(TicketContent maskedTicket, CancellationToken ct)
    {
        var request = new ChatRequest(
            _model,
            new[] { new ChatMessage("user", TriagePrompt.Build(maskedTicket)) },
            new ResponseFormat("json_object"));

        var response = await _httpClient.PostAsJsonAsync("/v1/chat/completions", request, ct);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content
            ?? throw new InvalidOperationException("OpenAI returned no completion content.");

        return TriagePrompt.Parse(content);
    }

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] ChatMessage[] Messages,
        [property: JsonPropertyName("response_format")] ResponseFormat ResponseFormat);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ResponseFormat([property: JsonPropertyName("type")] string Type);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] List<Choice>? Choices);

    private sealed record Choice([property: JsonPropertyName("message")] ChatMessage? Message);
}
