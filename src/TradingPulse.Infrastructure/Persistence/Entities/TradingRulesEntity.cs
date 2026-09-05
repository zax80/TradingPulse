using System.Text.Json;
using TradingPulse.Domain;

namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>Persistence model for <see cref="TradingRules"/>. Single row (<see cref="Id"/> is always <c>1</c>) - see <see cref="Rules.EfTradingRulesRepository"/>.</summary>
public sealed class TradingRulesEntity
{
    public int Id { get; set; }
    public decimal MaxNotionalPerOrder { get; set; }
    public decimal MaxQuantityPerOrder { get; set; }
    public decimal PriceDeviationThresholdPercent { get; set; }
    public bool DuplicateOrderIdCheckEnabled { get; set; }
    public bool SymbolWhitelistEnabled { get; set; }
    public required string SymbolWhitelistJson { get; set; }
    public decimal AutoTradingSpreadPercentThreshold { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static TradingRulesEntity FromDomain(int id, TradingRules rules) => new()
    {
        Id = id,
        MaxNotionalPerOrder = rules.MaxNotionalPerOrder,
        MaxQuantityPerOrder = rules.MaxQuantityPerOrder,
        PriceDeviationThresholdPercent = rules.PriceDeviationThresholdPercent,
        DuplicateOrderIdCheckEnabled = rules.DuplicateOrderIdCheckEnabled,
        SymbolWhitelistEnabled = rules.SymbolWhitelistEnabled,
        SymbolWhitelistJson = JsonSerializer.Serialize(rules.SymbolWhitelist),
        AutoTradingSpreadPercentThreshold = rules.AutoTradingSpreadPercentThreshold,
        UpdatedAt = rules.UpdatedAt,
    };

    public void UpdateFrom(TradingRules rules)
    {
        MaxNotionalPerOrder = rules.MaxNotionalPerOrder;
        MaxQuantityPerOrder = rules.MaxQuantityPerOrder;
        PriceDeviationThresholdPercent = rules.PriceDeviationThresholdPercent;
        DuplicateOrderIdCheckEnabled = rules.DuplicateOrderIdCheckEnabled;
        SymbolWhitelistEnabled = rules.SymbolWhitelistEnabled;
        SymbolWhitelistJson = JsonSerializer.Serialize(rules.SymbolWhitelist);
        AutoTradingSpreadPercentThreshold = rules.AutoTradingSpreadPercentThreshold;
        UpdatedAt = rules.UpdatedAt;
    }

    public TradingRules ToDomain() => new()
    {
        MaxNotionalPerOrder = MaxNotionalPerOrder,
        MaxQuantityPerOrder = MaxQuantityPerOrder,
        PriceDeviationThresholdPercent = PriceDeviationThresholdPercent,
        DuplicateOrderIdCheckEnabled = DuplicateOrderIdCheckEnabled,
        SymbolWhitelistEnabled = SymbolWhitelistEnabled,
        SymbolWhitelist = JsonSerializer.Deserialize<List<string>>(SymbolWhitelistJson) ?? [],
        AutoTradingSpreadPercentThreshold = AutoTradingSpreadPercentThreshold,
        UpdatedAt = UpdatedAt,
    };
}
