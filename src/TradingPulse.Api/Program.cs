using TradingPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TradingPulse.Api" }));

// TODO (Day 4): submit order, get/update rules, trade history, latest price, orders by symbol.

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
