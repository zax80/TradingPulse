namespace TradingPulse.Application.Abstractions;

/// <summary>
/// Atomically claims a <c>ClientOrderId</c> the first time it is used for a decided order, so that
/// two concurrent submissions for the same id - a race the earlier check-then-act
/// (<see cref="IOrderRepository.GetByClientOrderIdAsync"/> followed later by
/// <see cref="IOrderRepository.AddAsync"/>) cannot fully prevent on its own - can only ever have one
/// winner, and that winner survives a restart (unlike a plain in-memory cache would).
///
/// Deliberately a separate, narrow abstraction from <see cref="IOrderRepository"/>: the orders table
/// keeps a full audit row for every attempt, including rejected duplicates, and is therefore not
/// itself unique on ClientOrderId (see <c>TradingPulseDbContext</c>). This store answers only "who
/// got here first" - <c>OrderSubmissionService</c> uses the result to resolve a losing race exactly
/// as it would have resolved a sequential duplicate.
/// </summary>
public interface IClientOrderIdReservationStore
{
    /// <summary>
    /// Attempts to claim <paramref name="clientOrderId"/> for <paramref name="orderId"/> /
    /// <paramref name="decisionId"/>. Comparison is case-insensitive, matching
    /// <see cref="IOrderRepository.GetByClientOrderIdAsync"/>.
    /// </summary>
    Task<ClientOrderIdReservationResult> TryReserveAsync(
        string clientOrderId, Guid orderId, Guid decisionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of <see cref="IClientOrderIdReservationStore.TryReserveAsync"/>. When <see cref="Reserved"/>
/// is <c>false</c>, <see cref="ExistingOrderId"/> is the order that won the claim instead - look it up
/// with <see cref="IOrderRepository.GetByOrderIdAsync"/> to resolve the race.
/// </summary>
public readonly record struct ClientOrderIdReservationResult(bool Reserved, Guid ExistingOrderId, Guid ExistingDecisionId);
