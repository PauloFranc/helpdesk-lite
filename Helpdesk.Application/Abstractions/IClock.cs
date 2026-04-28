namespace Helpdesk.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

