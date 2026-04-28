namespace Helpdesk.Domain.Tickets;

public sealed record TicketComment(
    Guid Id,
    Guid TicketId,
    string Author,
    string Message,
    DateTimeOffset CreatedAtUtc);

