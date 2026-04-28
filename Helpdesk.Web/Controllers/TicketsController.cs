using System.Net.Http.Json;
using Helpdesk.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Helpdesk.Web.Controllers;

public sealed class TicketsController : Controller
{
    private readonly HttpClient _api;

    public TicketsController(IHttpClientFactory factory) => _api = factory.CreateClient("helpdesk-api");

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await _api.GetFromJsonAsync<List<TicketListItemDto>>("api/v1/tickets", ct) ?? new();
        return View(items);
    }

    [HttpGet]
    public IActionResult Create() => View(new CreateTicketVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTicketVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await _api.PostAsJsonAsync("api/v1/tickets", new
        {
            title = vm.Title,
            description = vm.Description,
            priority = vm.Priority,
            createdBy = vm.CreatedBy,
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", $"Erro ao criar ticket: {response.StatusCode}");
            return View(vm);
        }

        var created = await response.Content.ReadFromJsonAsync<CreateTicketResponse>(cancellationToken: ct);
        return RedirectToAction(nameof(Details), new { id = created?.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var ticket = await _api.GetFromJsonAsync<TicketDetailsDto>($"api/v1/tickets/{id}", ct);
        if (ticket is null) return NotFound();
        return View(ticket);
    }
}

