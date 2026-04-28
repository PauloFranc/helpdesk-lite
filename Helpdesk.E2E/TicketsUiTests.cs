using Microsoft.Playwright;
using Xunit;

namespace Helpdesk.E2E;

public sealed class TicketsUiTests
{
    [Fact]
    public async Task Can_create_ticket_via_web_ui()
    {
        var baseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5002";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        await using var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
        });

        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(baseUrl + "/Tickets/Create");

            // Selectores por label -- estaveis a mudancas de CSS e de layout
            await page.GetByLabel("Titulo").FillAsync("Impressora nao imprime");
            await page.GetByLabel("Descricao").FillAsync("Teste E2E");
            await page.GetByLabel("Criado por").FillAsync("customer@local");

            await page.GetByRole(AriaRole.Button, new() { Name = "Criar" }).ClickAsync();

            // Espera por estado real -- sem Task.Delay
            await page.WaitForURLAsync("**/Tickets/Details/**");

            var html = await page.ContentAsync();
            Assert.Contains("Impressora", html);
        }
        catch
        {
            Directory.CreateDirectory("screenshots");
            Directory.CreateDirectory("traces");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = "screenshots/falha.png",
                FullPage = true,
            });
            await context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = "traces/falha.zip",
            });
            throw;
        }
    }
}
