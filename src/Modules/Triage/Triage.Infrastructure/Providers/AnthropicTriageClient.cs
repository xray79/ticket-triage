using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Triage.Application;
using Triage.Application.Providers;

namespace Triage.Infrastructure.Providers;

public sealed class AnthropicTriageClient : ITriageLlmClient
{
    public string ProviderKey => "anthropic";

    private readonly HttpClient _httpClient;
    private readonly string _model;

    public AnthropicTriageClient(HttpClient httpClient, string apiKey, string model)
    {
        _httpClient = httpClient;
        _model = model;
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<TriageResult> TriageAsync(TicketContent maskedTicket, CancellationToken ct)
    {
        var request = new MessagesRequest(
            _model,
            1024,
            new[] { new ChatMessage("user", TriagePrompt.Build(maskedTicket)) });

        var response = await _httpClient.PostAsJsonAsync("/v1/messages", request, ct);
        response.EnsureSuccessStatusCode();

        var completion = await response.Content.ReadFromJsonAsync<MessagesResponse>(cancellationToken: ct);
        var content = completion?.Content?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Anthropic returned no content block.");

        return TriagePrompt.Parse(content);
    }

    private sealed record MessagesRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("messages")] ChatMessage[] Messages);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record MessagesResponse(
        [property: JsonPropertyName("content")] List<ContentBlock>? Content);

    private sealed record ContentBlock([property: JsonPropertyName("text")] string? Text);
}
