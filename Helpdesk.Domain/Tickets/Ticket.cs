namespace Helpdesk.Domain.Tickets;

public sealed class Ticket
{
    private readonly List<TicketComment> _comments = new();
    private readonly List<TicketStatusChange> _statusChanges = new();
    private readonly List<TicketActivity> _activities = new();

    private Ticket(
        Guid id,
        string title,
        string? description,
        TicketPriority priority,
        string createdBy,
        DateTimeOffset createdAtUtc,
        TicketStatus status,
        byte[] rowVersion)
    {
        Id = id;
        Title = title;
        Description = description;
        Priority = priority;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
        Status = status;
        RowVersion = rowVersion;
    }

    public Guid Id { get; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public string CreatedBy { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public IReadOnlyList<TicketComment> Comments => _comments;
    public IReadOnlyList<TicketStatusChange> StatusChanges => _statusChanges;
    public IReadOnlyList<TicketActivity> Activities => _activities;

    public static Ticket Create(
        string title,
        string? description,
        TicketPriority priority,
        string createdBy,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title é obrigatório.", nameof(title));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("CreatedBy é obrigatório.", nameof(createdBy));

        var ticket = new Ticket(
            id: Guid.NewGuid(),
            title: title.Trim(),
            description: string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            priority: priority,
            createdBy: createdBy.Trim(),
            createdAtUtc: createdAtUtc,
            status: TicketStatus.Open,
            rowVersion: Array.Empty<byte>());

        ticket._activities.Add(new TicketActivity(
            Id: Guid.NewGuid(),
            TicketId: ticket.Id,
            Type: "TicketCreated",
            PerformedBy: ticket.CreatedBy,
            PerformedAtUtc: createdAtUtc,
            Data: null));

        return ticket;
    }

    // Usado por repositórios de persistência para re-hidratar o aggregate.
    public static Ticket Rehydrate(
        Guid id,
        string title,
        string? description,
        TicketPriority priority,
        TicketStatus status,
        string createdBy,
        DateTimeOffset createdAtUtc,
        byte[]? rowVersion = null,
        IEnumerable<TicketComment>? comments = null,
        IEnumerable<TicketStatusChange>? statusChanges = null,
        IEnumerable<TicketActivity>? activities = null)
    {
        var ticket = new Ticket(id, title, description, priority, createdBy, createdAtUtc, status, rowVersion ?? Array.Empty<byte>());
        if (comments is not null) ticket._comments.AddRange(comments);
        if (statusChanges is not null) ticket._statusChanges.AddRange(statusChanges);
        if (activities is not null) ticket._activities.AddRange(activities);
        return ticket;
    }

    public void AddComment(string author, string message, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author é obrigatório.", nameof(author));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message é obrigatório.", nameof(message));

        _comments.Add(new TicketComment(
            Id: Guid.NewGuid(),
            TicketId: Id,
            Author: author.Trim(),
            Message: message.Trim(),
            CreatedAtUtc: createdAtUtc));

        _activities.Add(new TicketActivity(
            Id: Guid.NewGuid(),
            TicketId: Id,
            Type: "CommentAdded",
            PerformedBy: author.Trim(),
            PerformedAtUtc: createdAtUtc,
            Data: null));
    }

    public void ChangeStatus(TicketStatus next, string changedBy, DateTimeOffset changedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
            throw new ArgumentException("ChangedBy é obrigatório.", nameof(changedBy));

        if (!IsAllowed(Status, next))
            throw new InvalidOperationException($"Transição inválida: {Status} -> {next}");

        var prev = Status;
        Status = next;
        _statusChanges.Add(new TicketStatusChange(prev, next, changedBy.Trim(), changedAtUtc));

        _activities.Add(new TicketActivity(
            Id: Guid.NewGuid(),
            TicketId: Id,
            Type: "StatusChanged",
            PerformedBy: changedBy.Trim(),
            PerformedAtUtc: changedAtUtc,
            Data: $"{prev}->{next}"));
    }

    // Usado por repos de persistência após SaveChanges() em concorrência optimista.
    public void UpdateRowVersion(byte[] rowVersion)
    {
        RowVersion = rowVersion ?? Array.Empty<byte>();
    }

    private static bool IsAllowed(TicketStatus from, TicketStatus to) =>
        (from, to) switch
        {
            (TicketStatus.Open, TicketStatus.InProgress) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved) => true,
            (TicketStatus.Resolved, TicketStatus.Closed) => true,
            _ => false,
        };
}

