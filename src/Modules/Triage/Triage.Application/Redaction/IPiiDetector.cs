namespace Triage.Application.Redaction;

/// <summary>
/// One pass over a single field of free text. Implementations: Presidio (deterministic
/// regex/NER, primary) and Ollama (semantic/contextual second pass). The engine unions
/// the spans from every registered detector before masking.
/// </summary>
public interface IPiiDetector
{
    string Name { get; }
    Task<IReadOnlyList<(int Start, int Length, string EntityType)>> DetectAsync(
        string fieldName, string text, CancellationToken ct);
}
