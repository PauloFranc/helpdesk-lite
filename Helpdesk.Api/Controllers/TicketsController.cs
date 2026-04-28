using System.ComponentModel.DataAnnotations;
using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Tickets;
using Helpdesk.Domain.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Api.Controllers;

[ApiController]
[Route("api/v1/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly TicketService _service;
    private readonly ITicketRepository _repo;

    public TicketsController(TicketService service, ITicketRepository repo)
    {
        _service = service;
        _repo = repo;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<TicketListItemResponse>> Search(
        [FromQuery] string? q,
        [FromQuery] TicketStatus? status,
        [FromQuery] int? priority,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        TicketPriority? pr;
        try
        {
            pr = priority is null ? null : new TicketPriority(priority.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Problem(title: "Validation error", detail: "priority deve estar entre 1 e 3.", statusCode: 400);
        }

        var tickets = _repo.Search(q, status, pr, page, pageSize);
        return Ok(tickets.Select(TicketListItemResponse.FromDomain).ToList());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<TicketDetailsResponse> GetById(Guid id)
    {
        var ticket = _repo.Get(id);
        return ticket is null ? NotFound() : Ok(TicketDetailsResponse.FromDomain(ticket));
    }

    [HttpPost]
    public async Task<ActionResult<CreateTicketResponse>> Create([FromBody] CreateTicketRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        TicketPriority? priorityOverride;
        try
        {
            priorityOverride = req.Priority is null ? null : new TicketPriority(req.Priority.Value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Problem(title: "Validation error", detail: "Priority deve estar entre 1 e 3.", statusCode: 400);
        }

        var ticket = await _service.CreateAsync(req.Title, req.Description, priorityOverride, req.CreatedBy, ct);

        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, new CreateTicketResponse(ticket.Id));
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.AddCommentAsync(id, req.Author, req.Message, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/status")]
    [Authorize(Policy = "AgentOnly")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        await _service.ChangeStatusAsync(id, req.Status, req.ChangedBy, ct);
        return NoContent();
    }

    public sealed record CreateTicketRequest(
        [Required] string Title,
        string? Description,
        int? Priority,
        [Required] string CreatedBy);

    public sealed record AddCommentRequest(
        [Required] string Author,
        [Required] string Message);

    public sealed record ChangeStatusRequest(
        [Required] TicketStatus Status,
        [Required] string ChangedBy);

    public sealed record CreateTicketResponse(Guid Id);

    public sealed record TicketListItemResponse(Guid Id, string Title, TicketStatus Status, int Priority, DateTimeOffset CreatedAtUtc)
    {
        public static TicketListItemResponse FromDomain(Ticket t) =>
            new(t.Id, t.Title, t.Status, t.Priority.Value, t.CreatedAtUtc);
    }

    public sealed record TicketDetailsResponse(
        Guid Id,
        string Title,
        string? Description,
        TicketStatus Status,
        int Priority,
        string CreatedBy,
        DateTimeOffset CreatedAtUtc,
        IReadOnlyList<TicketCommentResponse> Comments,
        IReadOnlyList<TicketStatusChangeResponse> StatusChanges)
    {
        public static TicketDetailsResponse FromDomain(Ticket t) =>
            new(
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority.Value,
                t.CreatedBy,
                t.CreatedAtUtc,
                t.Comments.Select(c => new TicketCommentResponse(c.Author, c.Message, c.CreatedAtUtc)).ToList(),
                t.StatusChanges.Select(sc => new TicketStatusChangeResponse(sc.From, sc.To, sc.ChangedBy, sc.ChangedAtUtc)).ToList());
    }

    public sealed record TicketCommentResponse(string Author, string Message, DateTimeOffset CreatedAtUtc);
    public sealed record TicketStatusChangeResponse(TicketStatus From, TicketStatus To, string ChangedBy, DateTimeOffset ChangedAtUtc);
}
