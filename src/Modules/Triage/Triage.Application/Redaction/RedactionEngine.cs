namespace Triage.Application.Redaction;

/// <summary>
/// Runs every registered <see cref="IPiiDetector"/> over each field, unions the detected
/// spans (a missed PII leak is worse than over-redacting a harmless word, so we favor
/// recall), and replaces them with stable placeholder tokens. The mapping never leaves
/// this process — only the masked text is sent to any LLM, local or cloud.
/// </summary>
public sealed class RedactionEngine : IRedactionEngine
{
    private readonly IEnumerable<IPiiDetector> _detectors;

    public RedactionEngine(IEnumerable<IPiiDetector> detectors)
    {
        _detectors = detectors;
    }

    public async Task<RedactedTicket> RedactAsync(string subject, string body, CancellationToken ct)
    {
        var maskedSubject = await RedactFieldAsync("subject", subject, ct);
        var maskedBody = await RedactFieldAsync("body", body, ct);
        return new RedactedTicket(maskedSubject, maskedBody);
    }

    private async Task<RedactedField> RedactFieldAsync(string fieldName, string text, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(text))
            return new RedactedField(text, new Dictionary<string, string>());

        var allSpans = new List<(int Start, int Length, string EntityType)>();
        foreach (var detector in _detectors)
        {
            var spans = await detector.DetectAsync(fieldName, text, ct);
            allSpans.AddRange(spans);
        }

        // A detector reporting a span outside this field's bounds (wrong field, off-by-one, bad
        // model output) should lose that one span, not crash the whole redaction pass — silently
        // failing to redact one span is contained, but an exception here fails the ticket entirely.
        allSpans.RemoveAll(s => s.Start < 0 || s.Length <= 0 || s.Start + s.Length > text.Length);

        var merged = MergeOverlapping(allSpans);

        var mapping = new Dictionary<string, string>();
        var counters = new Dictionary<string, int>();
        var sb = new System.Text.StringBuilder();
        var cursor = 0;

        foreach (var span in merged.OrderBy(s => s.Start))
        {
            if (span.Start < cursor)
                continue; // defensive: skip any span that overlaps one already emitted

            sb.Append(text, cursor, span.Start - cursor);

            var count = counters.TryGetValue(span.EntityType, out var c) ? c + 1 : 1;
            counters[span.EntityType] = count;
            var placeholder = $"[{span.EntityType}_{count}]";

            mapping[placeholder] = text.Substring(span.Start, span.Length);
            sb.Append(placeholder);
            cursor = span.Start + span.Length;
        }

        sb.Append(text, cursor, text.Length - cursor);

        return new RedactedField(sb.ToString(), mapping);
    }

    private static List<(int Start, int Length, string EntityType)> MergeOverlapping(
        List<(int Start, int Length, string EntityType)> spans)
    {
        if (spans.Count == 0)
            return spans;

        var sorted = spans.OrderBy(s => s.Start).ThenByDescending(s => s.Length).ToList();
        var merged = new List<(int Start, int Length, string EntityType)>();
        var current = sorted[0];

        for (var i = 1; i < sorted.Count; i++)
        {
            var next = sorted[i];
            var currentEnd = current.Start + current.Length;

            if (next.Start <= currentEnd)
            {
                var newEnd = Math.Max(currentEnd, next.Start + next.Length);
                current = (current.Start, newEnd - current.Start, current.EntityType);
            }
            else
            {
                merged.Add(current);
                current = next;
            }
        }

        merged.Add(current);
        return merged;
    }
}
