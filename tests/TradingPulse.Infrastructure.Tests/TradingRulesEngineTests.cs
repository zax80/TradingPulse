using TradingPulse.Domain;
using TradingPulse.Domain.Enums;
using TradingPulse.Infrastructure.Rules;

namespace TradingPulse.Infrastructure.Tests;

public class TradingRulesEngineTests
{
    private readonly TradingRulesEngine _engine = new();

    [Fact]
    public void Evaluate_Accepts_WhenAllRulesPass()
    {
        var order = CreateOrder(price: 100m, quantity: 10m);
        var currentPrice = CreateSnapshot(currentMarketPrice: 100m);

        var decision = _engine.Evaluate(order, TradingRules.Default, currentPrice, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Accepted, decision.Status);
        Assert.Empty(decision.RejectionReasons);
    }

    [Fact]
    public void Evaluate_Rejects_WhenNotionalExceedsMax()
    {
        var rules = TradingRules.Default with { MaxNotionalPerOrder = 500m };
        var order = CreateOrder(price: 100m, quantity: 10m); // notional = 1000
        var currentPrice = CreateSnapshot(currentMarketPrice: 100m);

        var decision = _engine.Evaluate(order, rules, currentPrice, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Single(decision.RejectionReasons);
        Assert.Contains("exceeds the maximum of 500", decision.RejectionReasons[0]);
    }

    [Fact]
    public void Evaluate_Rejects_WhenQuantityExceedsMax()
    {
        var rules = TradingRules.Default with { MaxQuantityPerOrder = 5m };
        var order = CreateOrder(price: 1m, quantity: 10m);
        var currentPrice = CreateSnapshot(currentMarketPrice: 1m);

        var decision = _engine.Evaluate(order, rules, currentPrice, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Single(decision.RejectionReasons);
        Assert.Contains("Quantity 10", decision.RejectionReasons[0]);
    }

    [Fact]
    public void Evaluate_Rejects_WhenPriceDeviatesTooMuch()
    {
        var order = CreateOrder(price: 105m, quantity: 1m);
        var currentPrice = CreateSnapshot(currentMarketPrice: 100m); // 5% deviation, default threshold 0.8%

        var decision = _engine.Evaluate(order, TradingRules.Default, currentPrice, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Single(decision.RejectionReasons);
        Assert.Contains("deviates", decision.RejectionReasons[0]);
    }

    [Fact]
    public void Evaluate_Rejects_WhenNoCurrentPriceAvailable()
    {
        var order = CreateOrder(price: 1m, quantity: 1m);

        var decision = _engine.Evaluate(order, TradingRules.Default, currentPrice: null, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Single(decision.RejectionReasons);
        Assert.Contains("No current market price", decision.RejectionReasons[0]);
    }

    [Fact]
    public void Evaluate_Rejects_WhenDuplicateClientOrderId_AndCheckEnabled()
    {
        var order = CreateOrder(price: 1m, quantity: 1m);
        var currentPrice = CreateSnapshot(currentMarketPrice: 1m);

        var decision = _engine.Evaluate(order, TradingRules.Default, currentPrice, isDuplicateClientOrderId: true);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Single(decision.RejectionReasons);
        Assert.Contains("already been used", decision.RejectionReasons[0]);
    }

    [Fact]
    public void Evaluate_Accepts_Duplicate_WhenCheckDisabled()
    {
        var rules = TradingRules.Default with { DuplicateOrderIdCheckEnabled = false };
        var order = CreateOrder(price: 1m, quantity: 1m);
        var currentPrice = CreateSnapshot(currentMarketPrice: 1m);

        var decision = _engine.Evaluate(order, rules, currentPrice, isDuplicateClientOrderId: true);

        Assert.Equal(DecisionStatus.Accepted, decision.Status);
    }

    [Fact]
    public void Evaluate_Rejects_WhenSymbolNotWhitelisted()
    {
        var rules = TradingRules.Default with { SymbolWhitelistEnabled = true, SymbolWhitelist = ["GBPUSD"] };
        var order = CreateOrder(price: 1m, quantity: 1m, symbol: "EURUSD");
        var currentPrice = CreateSnapshot(currentMarketPrice: 1m, symbol: "EURUSD");

        var decision = _engine.Evaluate(order, rules, currentPrice, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Single(decision.RejectionReasons);
        Assert.Contains("not on the whitelist", decision.RejectionReasons[0]);
    }

    [Fact]
    public void Evaluate_Accepts_WhitelistedSymbol_CaseInsensitive()
    {
        var rules = TradingRules.Default with { SymbolWhitelistEnabled = true, SymbolWhitelist = ["eurusd"] };
        var order = CreateOrder(price: 1m, quantity: 1m, symbol: "EURUSD");
        var currentPrice = CreateSnapshot(currentMarketPrice: 1m, symbol: "EURUSD");

        var decision = _engine.Evaluate(order, rules, currentPrice, isDuplicateClientOrderId: false);

        Assert.Equal(DecisionStatus.Accepted, decision.Status);
    }

    [Fact]
    public void Evaluate_CollectsMultipleRejectionReasons()
    {
        var rules = TradingRules.Default with { MaxQuantityPerOrder = 5m };
        var order = CreateOrder(price: 1m, quantity: 10m);
        var currentPrice = CreateSnapshot(currentMarketPrice: 1m);

        var decision = _engine.Evaluate(order, rules, currentPrice, isDuplicateClientOrderId: true);

        Assert.Equal(DecisionStatus.Rejected, decision.Status);
        Assert.Equal(2, decision.RejectionReasons.Count);
    }

    private static Order CreateOrder(decimal price, decimal quantity, string symbol = "EURUSD", string clientOrderId = "ORD-1") =>
        Order.CreateUserSubmitted(clientOrderId, symbol, OrderSide.Buy, OrderType.Limit, price, quantity, DateTimeOffset.UtcNow);

    private static PriceSnapshot CreateSnapshot(decimal currentMarketPrice, string symbol = "EURUSD") =>
        new(symbol, currentMarketPrice, currentMarketPrice, currentMarketPrice, 0m, 0m, null, DateTimeOffset.UtcNow);
}
