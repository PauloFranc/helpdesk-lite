using Helpdesk.Application.Abstractions;

namespace Helpdesk.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

