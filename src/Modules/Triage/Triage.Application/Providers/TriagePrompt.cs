using System.Text.Json;
using System.Text.Json.Serialization;

namespace Triage.Application.Providers;

/// <summary>Shared prompt/response contract across every provider client so they stay interchangeable.</summary>
public static class TriagePrompt
{
    private const string Instructions = """
        You are a support ticket triage assistant. The ticket text below has PII replaced with
        placeholders like [PERSON_1] — keep any placeholders exactly as-is in your reply, do not
        try to guess the original value.

        Classify the ticket and draft a first-response reply. Respond with ONLY JSON matching:
        {"category": "<billing|technical|account|general>", "priority": "<low|medium|high|urgent>",
          "summary": "<one sentence>", "draftReply": "<a short, polite first-response draft>"}
        """;

    public static string Build(TicketContent ticket) =>
        $"{Instructions}\n\nSubject: {ticket.Subject}\nBody: {ticket.Body}";

    public static TriageResult Parse(string modelResponse)
    {
        var jsonStart = modelResponse.IndexOf('{');
        var jsonEnd = modelResponse.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd < jsonStart)
            throw new InvalidOperationException("Provider response did not contain a JSON object.");

        var jsonSlice = modelResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
        var parsed = JsonSerializer.Deserialize<TriageJson>(jsonSlice)
            ?? throw new InvalidOperationException("Could not parse the provider's triage JSON.");

        return new TriageResult(
            parsed.Category ?? "general",
            parsed.Priority ?? "medium",
            parsed.Summary ?? string.Empty,
            parsed.DraftReply ?? string.Empty);
    }

    private sealed record TriageJson(
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("priority")] string? Priority,
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("draftReply")] string? DraftReply);
}
