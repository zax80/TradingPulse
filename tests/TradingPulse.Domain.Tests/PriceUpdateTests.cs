namespace TradingPulse.Domain.Tests;

public class PriceUpdateTests
{
    [Fact]
    public void TryCreate_ReturnsTrue_ForValidPrices()
    {
        var created = PriceUpdate.TryCreate("EURUSD", 1.0800m, 1.0805m, DateTimeOffset.UtcNow, out var update, out var error);

        Assert.True(created);
        Assert.Null(error);
        Assert.Equal("EURUSD", update.Symbol);
        Assert.Equal(1.0800m, update.BidPrice);
        Assert.Equal(1.0805m, update.AskPrice);
    }

    [Fact]
    public void TryCreate_ReturnsFalse_WhenBidIsNotLessThanAsk()
    {
        var created = PriceUpdate.TryCreate("EURUSD", 1.0805m, 1.0800m, DateTimeOffset.UtcNow, out _, out var error);

        Assert.False(created);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_ReturnsFalse_WhenBidEqualsAsk()
    {
        var created = PriceUpdate.TryCreate("EURUSD", 1.0800m, 1.0800m, DateTimeOffset.UtcNow, out _, out var error);

        Assert.False(created);
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    public void TryCreate_ReturnsFalse_ForNonPositivePrices(decimal bid, decimal ask)
    {
        var created = PriceUpdate.TryCreate("EURUSD", bid, ask, DateTimeOffset.UtcNow, out _, out var error);

        Assert.False(created);
        Assert.NotNull(error);
    }

    [Fact]
    public void TryCreate_ReturnsFalse_ForMissingSymbol()
    {
        var created = PriceUpdate.TryCreate("   ", 1.0800m, 1.0805m, DateTimeOffset.UtcNow, out _, out var error);

        Assert.False(created);
        Assert.NotNull(error);
    }
}
