using FluentAssertions;
using Tickets.Domain;
using Xunit;

namespace Tickets.Tests;

public sealed class TicketTests
{
    [Fact]
    public void Create_with_valid_input_raises_TicketCreated_and_starts_New()
    {
        var result = Ticket.Create("Cannot log in", "My password isn't working", "customer@example.com", "local", Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TicketStatus.New);
        result.Value.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TicketCreated");
    }

    [Theory]
    [InlineData("", "body", "a@b.com")]
    [InlineData("subject", "", "a@b.com")]
    [InlineData("subject", "body", "")]
    public void Create_rejects_missing_required_fields(string subject, string body, string email)
    {
        var result = Ticket.Create(subject, body, email, "local", Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ApplyTriageResult_transitions_to_Triaged_and_stores_outcome()
    {
        var ticket = Ticket.Create("Subject", "Body", "a@b.com", "local", Guid.NewGuid()).Value;
        var outcome = new TriageOutcome("billing", "high", "summary", "draft", "local", false, DateTimeOffset.UtcNow);

        var result = ticket.ApplyTriageResult(outcome);

        result.IsSuccess.Should().BeTrue();
        ticket.Status.Should().Be(TicketStatus.Triaged);
        ticket.Triage.Should().Be(outcome);
    }

    [Fact]
    public void ApplyTriageResult_on_resolved_ticket_fails()
    {
        var ticket = Ticket.Create("Subject", "Body", "a@b.com", "local", Guid.NewGuid()).Value;
        ticket.Resolve();

        var result = ticket.ApplyTriageResult(new TriageOutcome("billing", "high", "s", "d", "local", false, DateTimeOffset.UtcNow));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Resolve_is_not_idempotent_a_second_call_fails()
    {
        var ticket = Ticket.Create("Subject", "Body", "a@b.com", "local", Guid.NewGuid()).Value;
        ticket.Resolve().IsSuccess.Should().BeTrue();

        var second = ticket.Resolve();

        second.IsFailure.Should().BeTrue();
    }
}
