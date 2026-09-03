using Microsoft.Extensions.DependencyInjection;
using TradingPulse.Application.Abstractions;
using TradingPulse.Infrastructure.Pricing;

namespace TradingPulse.Infrastructure;

/// <summary>Composition root for the Infrastructure layer's service registrations.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IPriceStateRepository, InMemoryPriceStateRepository>();
        services.AddSingleton<IPricingEngine, SimulatedPricingEngine>();
        services.AddHostedService<PriceTickProcessor>();

        // Day 3 - ITradingRulesEngine, IAutoTradingService
        // Day 4 - EF Core DbContext, IOrderRepository, ITradingRulesRepository (persistence)
        return services;
    }
}
