using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Tickets;
using Helpdesk.Domain.Tickets;
using Xunit;

namespace Helpdesk.Application.Tests.Tickets;

public sealed class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_persists_ticket_and_notifies()
    {
        var repo = new FakeRepo();
        var clock = new FakeClock(DateTimeOffset.Parse("2026-01-01T10:00:00Z"));
        var log = new BufferLogger();
        var notifications = new BufferNotifications();
        var factory = new TicketFactory(new DefaultPriorityCalculator());

        var service = new TicketService(repo, clock, factory, notifications, log);

        var ticket = await service.CreateAsync("t1", "d1", priorityOverride: null, createdBy: "u1", CancellationToken.None);

        Assert.NotEqual(Guid.Empty, ticket.Id);
        Assert.NotNull(repo.Get(ticket.Id));
        Assert.Single(notifications.Messages);
    }

    private sealed class FakeRepo : ITicketRepository
    {
        private readonly Dictionary<Guid, Ticket> _store = new();

        public Ticket? Get(Guid id) => _store.TryGetValue(id, out var t) ? t : null;
        public void Upsert(Ticket ticket) => _store[ticket.Id] = ticket;
        public IReadOnlyList<Ticket> Search(string? text = null, TicketStatus? status = null, TicketPriority? priority = null, int page = 1, int pageSize = 50) =>
            _store.Values.ToList();
        public IReadOnlyList<Ticket> OpenOlderThan(int days, DateTimeOffset nowUtc) => _store.Values.ToList();
        public IReadOnlyDictionary<TicketStatus, int> CountByStatus() => _store.Values.GroupBy(t => t.Status).ToDictionary(g => g.Key, g => g.Count());
    }

    private sealed class FakeClock : IClock
    {
        public FakeClock(DateTimeOffset now) => UtcNow = now;
        public DateTimeOffset UtcNow { get; }
    }

    private sealed class BufferNotifications : INotificationGateway
    {
        public List<string> Messages { get; } = new();
        public Task NotifyAsync(string channel, string message, CancellationToken ct)
        {
            Messages.Add($"[{channel}] {message}");
            return Task.CompletedTask;
        }
    }

    private sealed class BufferLogger : IAppLogger
    {
        public List<string> Lines { get; } = new();
        public void Info(string message) => Lines.Add("INFO " + message);
        public void Warn(string message) => Lines.Add("WARN " + message);
        public void Error(string message) => Lines.Add("ERROR " + message);
    }
}
