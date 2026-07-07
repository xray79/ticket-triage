namespace Triage.Application.Providers;

public sealed record TriageAttempt(TriageResult Result, string Provider, bool WasFallback);
