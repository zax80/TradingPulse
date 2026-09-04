using TradingPulse.Application.Abstractions;
using TradingPulse.Application.Models;
using TradingPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TradingPulse.Api" }));

// Temporary diagnostic endpoints for Day 2/3 - replaced by the spec-compliant
// API (GET /api/prices/{symbol}, GET /api/orders/history, ...) on Day 4.
app.MapGet("/debug/prices", async (IPriceStateRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetAllLatestAsync(ct)));

app.MapGet("/debug/orders", async (IOrderRepository repository, CancellationToken ct) =>
    Results.Ok(await repository.GetHistoryAsync(new OrderHistoryFilter(), ct)));

// TODO (Day 4): submit order, get/update rules, trade history, latest price, orders by symbol.

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
