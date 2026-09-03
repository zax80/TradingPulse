using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TradingPulse.Api" }));

// Temporary diagnostic endpoint for Day 2 - replaced by the spec-compliant
// GET /api/prices/{symbol} (and friends) on Day 4.
app.MapGet("/debug/prices", async (IPriceStateRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetAllLatestAsync(ct)));

// TODO (Day 4): submit order, get/update rules, trade history, latest price, orders by symbol.

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
