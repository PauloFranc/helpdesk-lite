using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Tickets;

public sealed class TicketService
{
    private readonly ITicketRepository _repo;
    private readonly IClock _clock;
    private readonly TicketFactory _factory;
    private readonly INotificationGateway _notifications;
    private readonly IAppLogger _log;

    public TicketService(ITicketRepository repo, IClock clock, TicketFactory factory, INotificationGateway notifications, IAppLogger log)
    {
        _repo = repo;
        _clock = clock;
        _factory = factory;
        _notifications = notifications;
        _log = log;
    }

    public async Task<Ticket> CreateAsync(string title, string? description, TicketPriority? priorityOverride, string createdBy, CancellationToken ct)
    {
        var ticket = _factory.Create(title, description, priorityOverride, createdBy, _clock.UtcNow);
        _repo.Upsert(ticket);

        _log.Info($"Ticket criado {ticket.Id} priority={ticket.Priority} title=\"{ticket.Title}\"");
        await _notifications.NotifyAsync("email", $"Ticket criado: {ticket.Id} ({ticket.Title})", ct);
        return ticket;
    }

    public async Task<Ticket> AddCommentAsync(Guid ticketId, string author, string message, CancellationToken ct)
    {
        var ticket = _repo.Get(ticketId) ?? throw new InvalidOperationException("Ticket não encontrado.");
        ticket.AddComment(author, message, _clock.UtcNow);
        _repo.Upsert(ticket);

        _log.Info($"AddComment id={ticket.Id} author=\"{author}\"");
        await _notifications.NotifyAsync("slack", $"Novo comentário no ticket {ticket.Id} por {author}", ct);
        return ticket;
    }

    public async Task<Ticket> ChangeStatusAsync(Guid ticketId, TicketStatus next, string changedBy, CancellationToken ct)
    {
        var ticket = _repo.Get(ticketId) ?? throw new InvalidOperationException("Ticket não encontrado.");
        ticket.ChangeStatus(next, changedBy, _clock.UtcNow);
        _repo.Upsert(ticket);

        _log.Info($"ChangeStatus id={ticket.Id} status={ticket.Status}");
        await _notifications.NotifyAsync("email", $"Ticket {ticket.Id} mudou para {ticket.Status}", ct);
        return ticket;
    }
}
