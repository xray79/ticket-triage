namespace Tickets.Domain;

/// <summary>
/// A denormalized copy of the triage result, written into the Tickets schema when the
/// module consumes Triage's published event. Tickets never queries Triage's database.
/// </summary>
public sealed record TriageOutcome(
    string Category,
    string Priority,
    string Summary,
    string DraftReply,
    string Provider,
    bool WasFallback,
    DateTimeOffset TriagedAtUtc);
