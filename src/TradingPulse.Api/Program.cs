using System.Text.Json.Serialization;
using TradingPulse.Api.Endpoints;
using TradingPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "TradingPulse.Api" }));

app.MapOrderEndpoints();
app.MapRulesEndpoints();
app.MapPriceEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
