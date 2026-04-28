using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Abstractions;

public interface ITicketRepository
{
    Ticket? Get(Guid id);
    void Upsert(Ticket ticket);

    IReadOnlyList<Ticket> Search(string? text = null, TicketStatus? status = null, TicketPriority? priority = null, int page = 1, int pageSize = 50);
    IReadOnlyList<Ticket> OpenOlderThan(int days, DateTimeOffset nowUtc);
    IReadOnlyDictionary<TicketStatus, int> CountByStatus();
}
