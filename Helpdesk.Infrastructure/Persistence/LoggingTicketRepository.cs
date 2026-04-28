using Helpdesk.Domain.Tickets;
using Helpdesk.Application.Abstractions;

namespace Helpdesk.Infrastructure.Persistence;

public sealed class LoggingTicketRepository : ITicketRepository
{
    private readonly ITicketRepository _inner;
    private readonly IAppLogger _log;

    public LoggingTicketRepository(ITicketRepository inner, IAppLogger log)
    {
        _inner = inner;
        _log = log;
    }

    public Ticket? Get(Guid id)
    {
        _log.Info($"Repo.Get {id}");
        return _inner.Get(id);
    }

    public void Upsert(Ticket ticket)
    {
        _log.Info($"Repo.Upsert {ticket.Id}");
        _inner.Upsert(ticket);
    }

    public IReadOnlyList<Ticket> Search(string? text = null, TicketStatus? status = null, TicketPriority? priority = null, int page = 1, int pageSize = 50)
    {
        var s = status?.ToString() ?? "*";
        var p = priority?.ToString() ?? "*";
        _log.Info($"Repo.Search q=\"{text}\" status={s} priority={p} page={page} size={pageSize}");
        return _inner.Search(text, status, priority, page, pageSize);
    }

    public IReadOnlyList<Ticket> OpenOlderThan(int days, DateTimeOffset nowUtc)
    {
        _log.Info($"Repo.OpenOlderThan days={days}");
        return _inner.OpenOlderThan(days, nowUtc);
    }

    public IReadOnlyDictionary<TicketStatus, int> CountByStatus()
    {
        _log.Info("Repo.CountByStatus");
        return _inner.CountByStatus();
    }
}
