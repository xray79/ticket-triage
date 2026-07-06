namespace Tickets.Domain;

public enum TicketStatus
{
    New = 0,
    Triaged = 1,
    InProgress = 2,
    Resolved = 3,
    TriageFailed = 4
}
