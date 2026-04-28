using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Helpdesk.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Helpdesk.IntegrationTests;

public sealed class TicketsApiTests
{
    [Fact]
    public async Task Get_tickets_without_token_returns_401()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/tickets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Can_create_and_list_ticket_with_agent_token()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        await client.AuthenticateAsync("agent@local");

        var create = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            title = "Impressora não imprime",
            description = "Teste",
            priority = (int?)null,
            createdBy = "customer@local",
        });
        create.EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<List<TicketListItem>>("/api/v1/tickets");
        Assert.NotNull(list);
        Assert.Contains(list!, t => t.Title.Contains("Impressora", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Change_status_with_customer_token_returns_403()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        await client.AuthenticateAsync("agent@local");
        var create = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            title = "Ticket para testar autorização",
            description = "Teste 403",
            priority = (int?)null,
            createdBy = "agent@local",
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedTicket>();

        await client.AuthenticateAsync("customer@local");
        var statusResp = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{created!.Id}/status",
            new { status = "InProgress", changedBy = "customer@local" });

        Assert.Equal(HttpStatusCode.Forbidden, statusResp.StatusCode);
    }

    [Fact]
    public async Task Agent_can_change_ticket_status()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        await client.AuthenticateAsync("agent@local");
        var create = await client.PostAsJsonAsync("/api/v1/tickets", new
        {
            title = "Ticket para mudar estado",
            description = "Teste 204",
            priority = (int?)null,
            createdBy = "agent@local",
        });
        create.EnsureSuccessStatusCode();
        var created = await create.Content.ReadFromJsonAsync<CreatedTicket>();

        var statusResp = await client.PostAsJsonAsync(
            $"/api/v1/tickets/{created!.Id}/status",
            new { status = "InProgress", changedBy = "agent@local" });

        Assert.Equal(HttpStatusCode.NoContent, statusResp.StatusCode);
    }

    [Fact]
    public async Task Liveness_endpoint_returns_200()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_endpoint_returns_200()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Response_contains_correlation_id_header()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", "test-correlation-123");

        var response = await client.SendAsync(request);

        Assert.Equal("test-correlation-123", response.Headers.GetValues("X-Correlation-Id").First());
    }

    [Fact]
    public async Task Response_generates_correlation_id_when_none_provided()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Correlation-Id"));
        Assert.NotEmpty(response.Headers.GetValues("X-Correlation-Id").First());
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Helpdesk:Storage"] = "Memory",
                    ["Jwt:Key"]      = "test-secret-key-exactly-32-bytes!!",
                    ["Jwt:Issuer"]   = "helpdesk-lite",
                    ["Jwt:Audience"] = "helpdesk-lite",
                });
            });
        }
    }

    private sealed record TicketListItem(Guid Id, string Title, string Status, int Priority, DateTimeOffset CreatedAtUtc);
    private sealed record CreatedTicket(Guid Id);
}

file static class HttpClientAuthExtensions
{
    public static async Task AuthenticateAsync(this HttpClient client, string email)
    {
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = "Passw0rd!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>()
                   ?? throw new InvalidOperationException("Login nao devolveu token.");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.AccessToken);
    }

    private sealed record LoginResponse(string AccessToken);
}
