namespace Helpdesk.Domain.Tickets;

public sealed record TicketActivity(
    Guid Id,
    Guid TicketId,
    string Type,
    string PerformedBy,
    DateTimeOffset PerformedAtUtc,
    string? Data);

