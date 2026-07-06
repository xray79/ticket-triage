using FluentAssertions;
using Triage.Application.Redaction;
using Xunit;

namespace Triage.Tests;

public sealed class RedactionEngineTests
{
    private sealed class FakeDetector : IPiiDetector
    {
        private readonly IReadOnlyList<(int Start, int Length, string EntityType)> _spans;
        public string Name => "fake";

        public FakeDetector(IReadOnlyList<(int, int, string)> spans) => _spans = spans;

        public Task<IReadOnlyList<(int Start, int Length, string EntityType)>> DetectAsync(string fieldName, string text, CancellationToken ct) =>
            Task.FromResult(fieldName == "body" ? _spans : Array.Empty<(int, int, string)>());
    }

    [Fact]
    public async Task RedactAsync_masks_a_single_detected_span_and_can_rehydrate_it()
    {
        var body = "My email is john@example.com, please help.";
        var emailStart = body.IndexOf("john@example.com", StringComparison.Ordinal);
        var detector = new FakeDetector(new[] { (emailStart, "john@example.com".Length, "EMAIL") });
        var engine = new RedactionEngine(new[] { detector });

        var redacted = await engine.RedactAsync("Subject", body, CancellationToken.None);

        redacted.Body.MaskedText.Should().Be("My email is [EMAIL_1], please help.");
        redacted.RehydrateBody("Draft mentioning [EMAIL_1] here").Should().Be("Draft mentioning john@example.com here");
    }

    [Fact]
    public async Task RedactAsync_unions_spans_from_multiple_detectors_without_double_counting_overlap()
    {
        var body = "Contact John Smith at john@example.com.";
        var nameStart = body.IndexOf("John Smith", StringComparison.Ordinal);
        var emailStart = body.IndexOf("john@example.com", StringComparison.Ordinal);

        var presidio = new FakeDetector(new[] { (emailStart, "john@example.com".Length, "EMAIL") });
        var ollama = new FakeDetector(new[] { (nameStart, "John Smith".Length, "PERSON") });
        var engine = new RedactionEngine(new IPiiDetector[] { presidio, ollama });

        var redacted = await engine.RedactAsync("Subject", body, CancellationToken.None);

        redacted.Body.MaskedText.Should().Be("Contact [PERSON_1] at [EMAIL_1].");
    }

    [Fact]
    public async Task RedactAsync_merges_overlapping_spans_from_different_detectors_into_one_placeholder()
    {
        var body = "Reach out to Sarah Jones directly.";
        var wideStart = body.IndexOf("Sarah Jones", StringComparison.Ordinal);
        var narrowStart = body.IndexOf("Jones", StringComparison.Ordinal);

        var presidio = new FakeDetector(new[] { (wideStart, "Sarah Jones".Length, "PERSON") });
        var ollama = new FakeDetector(new[] { (narrowStart, "Jones".Length, "PERSON") });
        var engine = new RedactionEngine(new IPiiDetector[] { presidio, ollama });

        var redacted = await engine.RedactAsync("Subject", body, CancellationToken.None);

        redacted.Body.MaskedText.Should().Be("Reach out to [PERSON_1] directly.");
    }

    [Fact]
    public async Task RedactAsync_ignores_an_out_of_bounds_span_instead_of_throwing()
    {
        var body = "Short body.";
        var detector = new FakeDetector(new[] { (0, 500, "PERSON") }); // length far exceeds body
        var engine = new RedactionEngine(new[] { detector });

        var redacted = await engine.RedactAsync("Subject", body, CancellationToken.None);

        redacted.Body.MaskedText.Should().Be(body);
    }

    [Fact]
    public async Task RedactAsync_returns_text_unchanged_when_no_detector_finds_anything()
    {
        var engine = new RedactionEngine(new[] { new FakeDetector(Array.Empty<(int, int, string)>()) });

        var redacted = await engine.RedactAsync("Subject", "Nothing sensitive here.", CancellationToken.None);

        redacted.Body.MaskedText.Should().Be("Nothing sensitive here.");
        redacted.Body.PlaceholderToOriginal.Should().BeEmpty();
    }
}
