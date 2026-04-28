namespace Helpdesk.Domain.Tickets;

public readonly record struct TicketPriority
{
    public static TicketPriority Low => new(1);
    public static TicketPriority Medium => new(2);
    public static TicketPriority High => new(3);

    public int Value { get; }

    public TicketPriority(int value)
    {
        if (value is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(value), "TicketPriority deve estar entre 1 e 3.");
        Value = value;
    }

    public override string ToString() =>
        Value switch
        {
            1 => "Low",
            2 => "Medium",
            3 => "High",
            _ => Value.ToString(),
        };
}

