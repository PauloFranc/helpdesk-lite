namespace Helpdesk.Domain.Tickets;

public sealed record TicketStatusChange(
    TicketStatus From,
    TicketStatus To,
    string ChangedBy,
    DateTimeOffset ChangedAtUtc);

