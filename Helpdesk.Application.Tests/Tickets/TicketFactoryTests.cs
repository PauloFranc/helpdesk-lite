using Helpdesk.Application.Tickets;
using Helpdesk.Domain.Tickets;
using Xunit;

namespace Helpdesk.Application.Tests.Tickets;

public sealed class TicketFactoryTests
{
    [Fact]
    public void KeywordPriorityCalculator_marks_urgent_as_high()
    {
        var calc = new KeywordPriorityCalculator();
        var factory = new TicketFactory(calc);

        var ticket = factory.Create("URGENTE: sistema em baixo", null, priorityOverride: null, createdBy: "u1", createdAtUtc: DateTimeOffset.UtcNow);

        Assert.Equal(TicketPriority.High, ticket.Priority);
    }

    [Fact]
    public void PriorityOverride_wins_over_strategy()
    {
        var calc = new KeywordPriorityCalculator();
        var factory = new TicketFactory(calc);

        var ticket = factory.Create("URGENTE: sistema em baixo", null, priorityOverride: TicketPriority.Low, createdBy: "u1", createdAtUtc: DateTimeOffset.UtcNow);

        Assert.Equal(TicketPriority.Low, ticket.Priority);
    }
}
