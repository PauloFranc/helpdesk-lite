using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Tickets;

public sealed class TicketFactory
{
    private readonly IPriorityCalculator _priorityCalculator;

    public TicketFactory(IPriorityCalculator priorityCalculator) => _priorityCalculator = priorityCalculator;

    public Ticket Create(
        string title,
        string? description,
        TicketPriority? priorityOverride,
        string createdBy,
        DateTimeOffset createdAtUtc)
    {
        var priority = priorityOverride ?? _priorityCalculator.Calculate(title, description);
        return Ticket.Create(title, description, priority, createdBy, createdAtUtc);
    }
}

