using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Tickets;
using Helpdesk.Domain.Tickets;
using Helpdesk.Infrastructure.Logging;
using Helpdesk.Infrastructure.Notifications;
using Helpdesk.Infrastructure.Persistence;
using Helpdesk.Infrastructure.Time;

static TicketPriority ReadPriority(string? input) =>
    input switch
    {
        "1" => TicketPriority.Low,
        "2" => TicketPriority.Medium,
        "3" => TicketPriority.High,
        _ => TicketPriority.Medium,
    };

static TicketPriority? ReadPriorityFilter(string? input) =>
    input switch
    {
        "" or null => null,
        "1" => TicketPriority.Low,
        "2" => TicketPriority.Medium,
        "3" => TicketPriority.High,
        _ => null,
    };

static TicketStatus? ReadStatusFilter(string? input) =>
    input switch
    {
        "" or null => null,
        "1" => TicketStatus.Open,
        "2" => TicketStatus.InProgress,
        "3" => TicketStatus.Resolved,
        "4" => TicketStatus.Closed,
        _ => null,
    };

static int ReadChoice(string? input) =>
    input switch
    {
        "1" => 1,
        "2" => 2,
        "3" => 3,
        "4" => 4,
        "5" => 5,
        "6" => 6,
        "7" => 7,
        "0" => 0,
        _ => -1,
    };

using var log = new FileLogger("helpdesk.log");
INotificationGateway notifications = new FakeNotificationGateway(log);
IClock clock = new SystemClock();

ITicketRepository repo = new InMemoryTicketRepository();

repo = new LoggingTicketRepository(repo, log);
var priorityCalculator = new KeywordPriorityCalculator();
var factory = new TicketFactory(priorityCalculator);

log.Info("Aplicação iniciada");
var service = new TicketService(repo, clock, factory, notifications, log);

using var appCts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    appCts.Cancel();
    Console.WriteLine();
    Console.WriteLine("Cancelamento solicitado (Ctrl+C).");
};

while (true)
{
    Console.WriteLine();
    Console.WriteLine("Helpdesk Lite — CLI (Sessão 09)");
    Console.WriteLine("1) Criar ticket");
    Console.WriteLine("2) Listar tickets (com filtros)");
    Console.WriteLine("3) Ver detalhes");
    Console.WriteLine("4) Relatório: contagem por estado");
    Console.WriteLine("5) Query: abertos há mais de N dias");
    Console.WriteLine("6) Adicionar comentário (assíncrono)");
    Console.WriteLine("7) Alterar estado");
    Console.WriteLine("0) Sair");
    Console.Write("> ");

    var choice = ReadChoice(Console.ReadLine());
    switch (choice)
    {
        case 1:
            await SafeRun(async ct => await CreateTicketAsync(service, ct), appCts.Token, log);
            break;
        case 2:
            ListTickets(repo, log);
            break;
        case 3:
            ShowDetails(repo, log);
            break;
        case 4:
            ShowCounts(repo, log);
            break;
        case 5:
            ShowOpenOlderThan(repo, clock, log);
            break;
        case 6:
            await SafeRun(async ct => await AddCommentAsync(service, ct), appCts.Token, log);
            break;
        case 7:
            await SafeRun(async ct => await ChangeStatusAsync(service, ct), appCts.Token, log);
            break;
        case 0:
            log.Info("Aplicação terminada");
            return;
        default:
            Console.WriteLine("Opção inválida.");
            break;
    }
}

static async Task SafeRun(Func<CancellationToken, Task> action, CancellationToken appToken, IAppLogger log)
{
    try
    {
        await action(appToken);
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Operação cancelada.");
        log.Warn("Operação cancelada");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro: {ex.Message}");
        log.Error(ex.ToString());
    }
}

static async Task CreateTicketAsync(TicketService service, CancellationToken appToken)
{
    Console.Write("Criado por: ");
    var createdBy = Console.ReadLine();

    Console.Write("Título: ");
    var title = Console.ReadLine();

    Console.Write("Descrição (opcional): ");
    var description = Console.ReadLine();

    Console.WriteLine("Prioridade: ENTER=auto (strategy) | 1) Low  2) Medium  3) High");
    Console.Write("> ");
    var priorityOverride = ReadPriorityFilter(Console.ReadLine());

    var ticket = await service.CreateAsync(title ?? "", description, priorityOverride, createdBy ?? "anonymous", appToken);

    Console.WriteLine($"Ticket criado: {ticket.Id}");
}


static void ListTickets(ITicketRepository repo, IAppLogger log)
{
    Console.Write("Pesquisa (texto; ENTER para ignorar): ");
    var text = Console.ReadLine();

    Console.WriteLine("Estado (ENTER para ignorar): 1) Open  2) InProgress  3) Resolved  4) Closed");
    Console.Write("> ");
    var status = ReadStatusFilter(Console.ReadLine());

    Console.WriteLine("Prioridade (ENTER para ignorar): 1) Low  2) Medium  3) High");
    Console.Write("> ");
    var priority = ReadPriorityFilter(Console.ReadLine());

    Console.Write("Página (default 1): ");
    var pageInput = Console.ReadLine();
    _ = int.TryParse(pageInput, out var page);
    page = page <= 0 ? 1 : page;

    Console.Write("Page size (default 20): ");
    var sizeInput = Console.ReadLine();
    _ = int.TryParse(sizeInput, out var pageSize);
    pageSize = pageSize <= 0 ? 20 : pageSize;

    var tickets = repo.Search(text, status, priority, page, pageSize);
    if (tickets.Count == 0)
    {
        Console.WriteLine("(sem resultados)");
        return;
    }

    foreach (var t in tickets)
        Console.WriteLine($"{t.Id} | {t.Status} | {t.Priority} | {t.Title}");

    log.Info($"Listagem executada results={tickets.Count} status={status?.ToString() ?? "*"} priority={priority?.ToString() ?? "*"} q=\"{text}\"");
}

static void ShowDetails(ITicketRepository repo, IAppLogger log)
{
    Console.Write("Ticket Id: ");
    var input = Console.ReadLine();
    if (!Guid.TryParse(input, out var id))
    {
        Console.WriteLine("Id inválido.");
        return;
    }

    var ticket = repo.Get(id);
    if (ticket is null)
    {
        Console.WriteLine("Ticket não encontrado.");
        log.Warn($"Details ticket_not_found id={id}");
        return;
    }
    log.Info($"Details id={ticket.Id}");

    Console.WriteLine();
    Console.WriteLine($"Id: {ticket.Id}");
    Console.WriteLine($"Título: {ticket.Title}");
    Console.WriteLine($"Descrição: {ticket.Description ?? "(sem descrição)"}");
    Console.WriteLine($"Estado: {ticket.Status}");
    Console.WriteLine($"Prioridade: {ticket.Priority}");
    Console.WriteLine($"Criado em: {ticket.CreatedAtUtc:o}");
    Console.WriteLine();
    Console.WriteLine("Comentários:");
    if (ticket.Comments.Count == 0)
        Console.WriteLine("(sem comentários)");
    else
        foreach (var c in ticket.Comments.OrderBy(x => x.CreatedAtUtc))
            Console.WriteLine($"- [{c.CreatedAtUtc:O}] {c.Author}: {c.Message}");
}

static void ShowCounts(ITicketRepository repo, IAppLogger log)
{
    var counts = repo.CountByStatus();
    if (counts.Count == 0)
    {
        Console.WriteLine("(sem tickets)");
        return;
    }

    foreach (var (status, count) in counts.OrderBy(kv => kv.Key))
        Console.WriteLine($"{status}: {count}");

    log.Info("Counts by status executado");
}

static void ShowOpenOlderThan(ITicketRepository repo, IClock clock, IAppLogger log)
{
    Console.Write("N dias: ");
    var input = Console.ReadLine();
    if (!int.TryParse(input, out var days) || days < 0)
    {
        Console.WriteLine("Número inválido.");
        return;
    }

    var tickets = repo.OpenOlderThan(days, clock.UtcNow);
    if (tickets.Count == 0)
    {
        Console.WriteLine("(sem resultados)");
        return;
    }

    foreach (var t in tickets)
        Console.WriteLine($"{t.CreatedAtUtc:yyyy-MM-dd} | {t.Status} | {t.Priority} | {t.Title} ({t.Id})");

    log.Info($"OpenOlderThan days={days} results={tickets.Count}");
}

static async Task AddCommentAsync(TicketService service, CancellationToken appToken)
{
    Console.Write("Ticket Id: ");
    var input = Console.ReadLine();
    if (!Guid.TryParse(input, out var id))
    {
        Console.WriteLine("Id inválido.");
        return;
    }

    Console.Write("Autor: ");
    var author = Console.ReadLine();

    Console.Write("Mensagem: ");
    var message = Console.ReadLine();

    await service.AddCommentAsync(id, author ?? "", message ?? "", appToken);
    Console.WriteLine("Comentário adicionado.");
}

static async Task ChangeStatusAsync(TicketService service, CancellationToken appToken)
{
    Console.Write("Ticket Id: ");
    var input = Console.ReadLine();
    if (!Guid.TryParse(input, out var id))
    {
        Console.WriteLine("Id inválido.");
        return;
    }

    Console.Write("Alterado por: ");
    var changedBy = Console.ReadLine();

    Console.WriteLine("Novo estado: 1) Open  2) InProgress  3) Resolved  4) Closed");
    Console.Write("> ");
    var status = ReadStatusFilter(Console.ReadLine());
    if (status is null)
    {
        Console.WriteLine("Estado inválido.");
        return;
    }

    var updated = await service.ChangeStatusAsync(id, status.Value, changedBy ?? "anonymous", appToken);
    Console.WriteLine($"Estado actualizado: {updated.Status}");
}
