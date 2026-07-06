namespace Triage.Application.Redaction;

/// <summary>A detected PII span within a field's text, [Start, Start+Length).</summary>
public sealed record PiiSpan(string Field, int Start, int Length, string EntityType, string DetectorSource);

/// <summary>
/// One field of a ticket after redaction: the masked text and the mapping from
/// placeholder token back to the original substring, so a rehydrated draft reply
/// can be produced without the mapping ever leaving the server.
/// </summary>
public sealed record RedactedField(string MaskedText, IReadOnlyDictionary<string, string> PlaceholderToOriginal);

public sealed record RedactedTicket(RedactedField Subject, RedactedField Body)
{
    public string RehydrateBody(string textWithPlaceholders)
    {
        var result = textWithPlaceholders;
        foreach (var (placeholder, original) in Body.PlaceholderToOriginal)
            result = result.Replace(placeholder, original);
        return result;
    }
}
