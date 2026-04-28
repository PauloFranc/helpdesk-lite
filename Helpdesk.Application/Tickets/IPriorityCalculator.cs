using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Tickets;

public interface IPriorityCalculator
{
    TicketPriority Calculate(string title, string? description);
}

