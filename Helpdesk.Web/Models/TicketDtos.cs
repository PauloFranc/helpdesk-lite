namespace Helpdesk.Web.Models;

public sealed record TicketListItemDto(Guid Id, string Title, string Status, int Priority, DateTimeOffset CreatedAtUtc);

public sealed record TicketDetailsDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    int Priority,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<TicketCommentDto> Comments,
    IReadOnlyList<TicketStatusChangeDto> StatusChanges);

public sealed record TicketCommentDto(string Author, string Message, DateTimeOffset CreatedAtUtc);
public sealed record TicketStatusChangeDto(string From, string To, string ChangedBy, DateTimeOffset ChangedAtUtc);
public sealed record CreateTicketResponse(Guid Id);

