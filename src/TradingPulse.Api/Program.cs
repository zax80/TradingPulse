using System.Text.Json.Serialization;
using TradingPulse.Api.Components;
using TradingPulse.Api.Endpoints;
using TradingPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TradingPulse.Api" }));

app.MapOrderEndpoints();
app.MapRulesEndpoints();
app.MapPriceEndpoints();

// Dashboard - reads/writes go straight through the same Application services the
// API endpoints use (no self-referencing HTTP call to its own API).
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Exposed for WebApplicationFactory-based integration tests. `public` isn't required
// (ASP0027) - WebApplicationFactory<Program> works against an internal partial class too.
partial class Program;
