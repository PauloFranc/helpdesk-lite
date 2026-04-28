using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Tickets;

public sealed class DefaultPriorityCalculator : IPriorityCalculator
{
    public TicketPriority Calculate(string title, string? description) => TicketPriority.Medium;
}

