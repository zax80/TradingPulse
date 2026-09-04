using Microsoft.Extensions.DependencyInjection;
using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure.Orders;
using TradingPulse.Infrastructure.Pricing;
using TradingPulse.Infrastructure.Rules;
using TradingPulse.Infrastructure.Trading;

namespace TradingPulse.Infrastructure;

/// <summary>Composition root for the Infrastructure layer's service registrations.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPriceStateRepository, InMemoryPriceStateRepository>();
        services.AddSingleton<ITradingRulesRepository, InMemoryTradingRulesRepository>();
        services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
        services.AddSingleton<IPricingEngine, SimulatedPricingEngine>();
        services.AddSingleton<ITradingRulesEngine, TradingRulesEngine>();
        services.AddSingleton<IAutoTradingService, SpreadBasedAutoTradingService>();
        services.AddHostedService<PriceTickProcessor>();

        // Day 4 - EF Core DbContext, replacing the in-memory repositories above
        return services;
    }
}
