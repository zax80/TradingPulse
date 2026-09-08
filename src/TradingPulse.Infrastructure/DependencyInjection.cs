using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure.Orders;
using TradingPulse.Infrastructure.Persistence;
using TradingPulse.Infrastructure.Persistence.Repositories;
using TradingPulse.Infrastructure.Pricing;
using TradingPulse.Infrastructure.Rules;
using TradingPulse.Infrastructure.Trading;

namespace TradingPulse.Infrastructure;

/// <summary>Composition root for the Infrastructure layer's service registrations.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:Default' - see appsettings.json / README 'How to Run'.");

        // Singleton services get a DbContext per operation via the factory, rather than
        // holding one directly - DbContext itself isn't thread-safe.
        services.AddDbContextFactory<TradingPulseDbContext>(options => options.UseNpgsql(connectionString));

        // Latest price stays in-memory for the hot tick path (Day 2); EfPriceSnapshotStore is
        // a separate, periodically-flushed durable copy - see PriceStatePersistenceService.
        services.AddSingleton<IPriceStateRepository, InMemoryPriceStateRepository>();
        services.AddSingleton<IPriceSnapshotStore, EfPriceSnapshotStore>();
        services.AddSingleton<ITradingRulesRepository, EfTradingRulesRepository>();
        services.AddSingleton<IOrderRepository, EfOrderRepository>();
        services.AddSingleton<IApiKeyRepository, EfApiKeyRepository>();

        services.AddSingleton<IPricingEngine, SimulatedPricingEngine>();
        services.AddSingleton<ITradingRulesEngine, TradingRulesEngine>();
        services.AddSingleton<IAutoTradingService, SpreadBasedAutoTradingService>();
        services.AddSingleton<IOrderSubmissionService, OrderSubmissionService>();

        services.AddHostedService<PriceTickProcessor>();
        services.AddHostedService<PriceStatePersistenceService>();

        return services;
    }

    /// <summary>Creates the database schema on startup if it doesn't exist yet. See README "Known Limitations" for why this is EnsureCreated, not migrations.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<TradingPulseDbContext>>();
        await using var db = await dbContextFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
    }
}
