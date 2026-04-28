using Helpdesk.Application.Abstractions;

namespace Helpdesk.Infrastructure.Notifications;

public sealed class FakeNotificationGateway : INotificationGateway
{
    private readonly IAppLogger _log;

    public FakeNotificationGateway(IAppLogger log) => _log = log;

    public async Task NotifyAsync(string channel, string message, CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
        _log.Info($"[{channel}] {message}");
    }
}

