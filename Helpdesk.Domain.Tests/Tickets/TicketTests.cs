using Helpdesk.Domain.Tickets;
using Xunit;

namespace Helpdesk.Domain.Tests.Tickets;

public sealed class TicketTests
{
    [Fact]
    public void Create_requires_title()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Ticket.Create("", "desc", TicketPriority.Low, "u1", DateTimeOffset.UtcNow));

        Assert.Contains("Title", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_requires_created_by()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            Ticket.Create("t", "desc", TicketPriority.Low, "", DateTimeOffset.UtcNow));

        Assert.Contains("CreatedBy", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangeStatus_enforces_allowed_transitions()
    {
        var ticket = Ticket.Create("t", null, TicketPriority.Low, "u1", DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            ticket.ChangeStatus(TicketStatus.Closed, "u2", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AddComment_requires_message()
    {
        var ticket = Ticket.Create("t", null, TicketPriority.Low, "u1", DateTimeOffset.UtcNow);
        Assert.Throws<ArgumentException>(() => ticket.AddComment("u2", "", DateTimeOffset.UtcNow));
    }
}
