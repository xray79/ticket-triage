using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Triage.Application.Providers;

/// <summary>
/// Per-provider triage telemetry — the highest-leverage dashboard in the system: fallback
/// rate per provider catches a degraded cloud provider before an agent complains. Exported
/// via whatever OpenTelemetry Metrics reader the Host wires up (Add-on B); this class only
/// owns instrument definitions, not the exporter.
/// </summary>
public sealed class TriageMetrics
{
    public const string MeterName = "TicketTriage.Triage";

    private readonly Counter<long> _attempts;
    private readonly Histogram<double> _duration;

    public TriageMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _attempts = meter.CreateCounter<long>(
            "triage.attempts",
            unit: "{attempt}",
            description: "Triage attempts, tagged by provider, whether it was a fallback, and outcome.");

        _duration = meter.CreateHistogram<double>(
            "triage.duration",
            unit: "s",
            description: "Wall-clock duration of a triage attempt, tagged by provider.");
    }

    public void RecordAttempt(string provider, bool wasFallback, bool succeeded, double durationSeconds)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "was_fallback", wasFallback },
            { "outcome", succeeded ? "success" : "failure" }
        };

        _attempts.Add(1, tags);
        _duration.Record(durationSeconds, new TagList { { "provider", provider } });
    }
}
