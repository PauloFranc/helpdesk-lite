using Helpdesk.Application.Abstractions;
using Helpdesk.Domain.Tickets;

namespace Helpdesk.Infrastructure.Persistence;

public sealed class InMemoryTicketRepository : ITicketRepository
{
    private readonly Dictionary<Guid, Ticket> _store = new();

    public void Upsert(Ticket ticket) => _store[ticket.Id] = ticket;

    public Ticket? Get(Guid id) => _store.TryGetValue(id, out var t) ? t : null;

    public IReadOnlyList<Ticket> Search(string? text = null, TicketStatus? status = null, TicketPriority? priority = null, int page = 1, int pageSize = 50)
    {
        var query = _store.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(t =>
                t.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                (t.Description?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (status is not null)
            query = query.Where(t => t.Status == status);

        if (priority is not null)
            query = query.Where(t => t.Priority == priority);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 50 : pageSize;

        return query
            .OrderByDescending(t => t.CreatedAtUtc)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    public IReadOnlyList<Ticket> OpenOlderThan(int days, DateTimeOffset nowUtc)
    {
        if (days < 0) throw new ArgumentOutOfRangeException(nameof(days));

        var threshold = nowUtc.AddDays(-days);
        return _store.Values
            .Where(t => t.Status is TicketStatus.Open or TicketStatus.InProgress)
            .Where(t => t.CreatedAtUtc < threshold)
            .OrderBy(t => t.CreatedAtUtc)
            .ThenBy(t => t.Id)
            .ToList();
    }

    public IReadOnlyDictionary<TicketStatus, int> CountByStatus() =>
        _store.Values
            .GroupBy(t => t.Status)
            .ToDictionary(g => g.Key, g => g.Count());
}
