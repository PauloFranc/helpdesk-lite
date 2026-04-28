using Helpdesk.Domain.Tickets;

namespace Helpdesk.Application.Tickets;

public sealed class KeywordPriorityCalculator : IPriorityCalculator
{
    public TicketPriority Calculate(string title, string? description)
    {
        var text = (title + " " + (description ?? "")).Trim();
        return text.Contains("URGENTE", StringComparison.OrdinalIgnoreCase) ? TicketPriority.High : TicketPriority.Medium;
    }
}

