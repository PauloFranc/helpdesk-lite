namespace Helpdesk.Application.Abstractions;

public interface INotificationGateway
{
    Task NotifyAsync(string channel, string message, CancellationToken ct);
}

