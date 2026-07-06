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

    private static readonly HashSet<string> ValidCategories =
        new(StringComparer.OrdinalIgnoreCase) { "billing", "technical", "account", "general" };

    private static readonly HashSet<string> ValidPriorities =
        new(StringComparer.OrdinalIgnoreCase) { "low", "medium", "high", "urgent" };

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

        // A prompt-injected or malfunctioning model can return a category/priority outside the
        // fixed vocabulary it was asked for — never trust it as-is, since it flows straight into
        // reporting breakdowns and UI badges that assume one of these four values each. See
        // docs/threat-model-ai-boundary.md.
        var category = parsed.Category is not null && ValidCategories.Contains(parsed.Category)
            ? parsed.Category.ToLowerInvariant()
            : "general";
        var priority = parsed.Priority is not null && ValidPriorities.Contains(parsed.Priority)
            ? parsed.Priority.ToLowerInvariant()
            : "medium";

        return new TriageResult(category, priority, parsed.Summary ?? string.Empty, parsed.DraftReply ?? string.Empty);
    }

    private sealed record TriageJson(
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("priority")] string? Priority,
        [property: JsonPropertyName("summary")] string? Summary,
        [property: JsonPropertyName("draftReply")] string? DraftReply);
}
