using Microsoft.Extensions.DependencyInjection;

namespace TradingPulse.Infrastructure;

/// <summary>Composition root for the Infrastructure layer's service registrations.</summary>
public static class DependencyInjection
{
    // Intentionally empty for now. Planned registrations:
    //   Day 2 - IPricingEngine, price-tick background processor
    //   Day 3 - ITradingRulesEngine, IAutoTradingService
    //   Day 4 - EF Core DbContext, IOrderRepository, ITradingRulesRepository, IPriceStateRepository
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services;
    }
}
