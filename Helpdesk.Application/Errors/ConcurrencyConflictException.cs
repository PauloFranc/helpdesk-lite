namespace Helpdesk.Application.Errors;

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception? inner = null) : base(message, inner) { }
}

