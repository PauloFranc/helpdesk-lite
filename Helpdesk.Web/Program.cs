var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient("helpdesk-api", client =>
{
    var baseUrl = builder.Configuration["Helpdesk:ApiBaseUrl"] ?? "http://localhost:5000/";
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);

    // Para sessões de auth: configurar um token fixo em appsettings.json para facilitar o arranque.
    var token = builder.Configuration["Helpdesk:ApiToken"];
    if (!string.IsNullOrWhiteSpace(token))
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
});

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tickets}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
