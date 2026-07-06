using Shared.Kernel;
using Tickets.Contracts.Events;

namespace Tickets.Domain;

public sealed class Ticket : AggregateRoot<Guid>
{
    private Ticket() { }

    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public string RequestedProvider { get; private set; } = "local";
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public TriageOutcome? Triage { get; private set; }

    public static Result<Ticket> Create(
        string subject,
        string body,
        string customerEmail,
        string requestedProvider,
        Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return Result.Failure<Ticket>(Error.Validation("Ticket.Subject", "Subject is required."));
        if (string.IsNullOrWhiteSpace(body))
            return Result.Failure<Ticket>(Error.Validation("Ticket.Body", "Body is required."));
        if (string.IsNullOrWhiteSpace(customerEmail))
            return Result.Failure<Ticket>(Error.Validation("Ticket.CustomerEmail", "Customer email is required."));

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            CustomerEmail = customerEmail.Trim(),
            RequestedProvider = string.IsNullOrWhiteSpace(requestedProvider) ? "local" : requestedProvider,
            Status = TicketStatus.New,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        ticket.Raise(new TicketCreated(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            ticket.Id,
            ticket.Subject,
            ticket.Body,
            ticket.CustomerEmail,
            ticket.RequestedProvider));

        return ticket;
    }

    public Result ApplyTriageResult(TriageOutcome outcome)
    {
        if (Status is TicketStatus.Resolved)
            return Result.Failure(Error.Conflict("Ticket.AlreadyResolved", "Cannot apply triage to a resolved ticket."));

        Triage = outcome;
        Status = TicketStatus.Triaged;
        return Result.Success();
    }

    public Result MarkTriageFailed()
    {
        if (Status is TicketStatus.Resolved)
            return Result.Failure(Error.Conflict("Ticket.AlreadyResolved", "Cannot fail triage on a resolved ticket."));

        Status = TicketStatus.TriageFailed;
        return Result.Success();
    }

    public Result AssignTo(Guid userId)
    {
        if (Status is TicketStatus.Resolved)
            return Result.Failure(Error.Conflict("Ticket.AlreadyResolved", "Cannot reassign a resolved ticket."));

        AssignedToUserId = userId;
        return Result.Success();
    }

    public Result Resolve()
    {
        if (Status is TicketStatus.Resolved)
            return Result.Failure(Error.Conflict("Ticket.AlreadyResolved", "Ticket is already resolved."));

        Status = TicketStatus.Resolved;
        Raise(new TicketResolved(Guid.NewGuid(), DateTimeOffset.UtcNow, Id));
        return Result.Success();
    }
}
