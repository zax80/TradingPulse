using TradingPulse.Infrastructure.Orders;

namespace TradingPulse.Infrastructure.Tests;

public class InMemoryClientOrderIdReservationStoreTests
{
    private readonly InMemoryClientOrderIdReservationStore _store = new();

    [Fact]
    public async Task TryReserveAsync_FirstCall_Succeeds()
    {
        var orderId = Guid.NewGuid();
        var decisionId = Guid.NewGuid();

        var result = await _store.TryReserveAsync("ORD-1", orderId, decisionId);

        Assert.True(result.Reserved);
        Assert.Equal(orderId, result.ExistingOrderId);
        Assert.Equal(decisionId, result.ExistingDecisionId);
    }

    [Fact]
    public async Task TryReserveAsync_SecondCall_SameId_Fails_AndReportsOriginalWinner()
    {
        var firstOrderId = Guid.NewGuid();
        var firstDecisionId = Guid.NewGuid();
        await _store.TryReserveAsync("ORD-2", firstOrderId, firstDecisionId);

        var result = await _store.TryReserveAsync("ORD-2", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Reserved);
        Assert.Equal(firstOrderId, result.ExistingOrderId);
        Assert.Equal(firstDecisionId, result.ExistingDecisionId);
    }

    [Fact]
    public async Task TryReserveAsync_IsCaseInsensitive_LikeGetByClientOrderIdAsync()
    {
        var orderId = Guid.NewGuid();
        await _store.TryReserveAsync("ord-3", orderId, Guid.NewGuid());

        var result = await _store.TryReserveAsync("ORD-3", Guid.NewGuid(), Guid.NewGuid());

        Assert.False(result.Reserved);
        Assert.Equal(orderId, result.ExistingOrderId);
    }

    [Fact]
    public async Task TryReserveAsync_DifferentIds_BothSucceed()
    {
        var first = await _store.TryReserveAsync("ORD-4", Guid.NewGuid(), Guid.NewGuid());
        var second = await _store.TryReserveAsync("ORD-5", Guid.NewGuid(), Guid.NewGuid());

        Assert.True(first.Reserved);
        Assert.True(second.Reserved);
    }
}
