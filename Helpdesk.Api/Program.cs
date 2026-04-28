using Helpdesk.Application.Abstractions;
using Helpdesk.Application.Errors;
using Helpdesk.Application.Tickets;
using Helpdesk.Infrastructure.Logging;
using Helpdesk.Infrastructure.Notifications;
using Helpdesk.Infrastructure.Persistence;
using Helpdesk.Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IAppLogger>(_ => new FileLogger("helpdesk.api.log"));
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<INotificationGateway>(sp => new FakeNotificationGateway(sp.GetRequiredService<IAppLogger>()));

builder.Services.AddScoped<IPriorityCalculator, KeywordPriorityCalculator>();
builder.Services.AddScoped<TicketFactory>();
builder.Services.AddScoped<TicketService>();

builder.Services.AddSingleton<InMemoryTicketRepository>();
builder.Services.AddSingleton<ITicketRepository>(sp => sp.GetRequiredService<InMemoryTicketRepository>());

// Health checks: liveness (always up) + readiness (verifica repositório)
builder.Services.AddHealthChecks()
    .AddCheck("repository", () => HealthCheckResult.Healthy("In-memory store ok"), tags: new[] { "ready" });

var jwtKey = builder.Configuration["Jwt:Key"] ?? "";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "helpdesk-lite";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "helpdesk-lite";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("AgentOnly", p => p.RequireRole("Agent"));
});

var app = builder.Build();

// Correlation ID — propaga ou gera, adiciona ao scope de logs
app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                        ?? Guid.NewGuid().ToString("N");
    ctx.Response.Headers["X-Correlation-Id"] = correlationId;
    var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        await next();
});

app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (ConcurrencyConflictException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Title = "Concurrency conflict",
            Detail = ex.Message,
            Status = StatusCodes.Status409Conflict,
            Type = "https://httpstatuses.com/409",
        });
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
    ResultStatusCodes = { [HealthStatus.Healthy] = StatusCodes.Status200OK },
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy]   = StatusCodes.Status200OK,
        [HealthStatus.Degraded]  = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable,
    },
});

app.MapControllers();
app.Run();

public partial class Program { }
