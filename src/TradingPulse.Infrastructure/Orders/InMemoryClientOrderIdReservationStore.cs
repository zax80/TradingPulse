using System.Collections.Concurrent;
using TradingPulse.Application.Abstractions;

namespace TradingPulse.Infrastructure.Orders;

/// <summary>
/// In-memory <see cref="IClientOrderIdReservationStore"/> using
/// <see cref="ConcurrentDictionary{TKey,TValue}.TryAdd"/> for the same atomic "first writer wins"
/// guarantee <see cref="Repositories.EfClientOrderIdReservationStore"/> gets from a database primary
/// key - lets tests exercise OrderSubmissionService's race-resolution path without a real Postgres
/// instance. Not restart-safe (by design, for tests); the EF implementation is what actually backs
/// the running service - see DependencyInjection.
/// </summary>
public sealed class InMemoryClientOrderIdReservationStore : IClientOrderIdReservationStore
{
    private readonly ConcurrentDictionary<string, (Guid OrderId, Guid DecisionId)> _reservations =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<ClientOrderIdReservationResult> TryReserveAsync(
        string clientOrderId, Guid orderId, Guid decisionId, CancellationToken cancellationToken = default)
    {
        if (_reservations.TryAdd(clientOrderId, (orderId, decisionId)))
        {
            return Task.FromResult(new ClientOrderIdReservationResult(Reserved: true, ExistingOrderId: orderId, ExistingDecisionId: decisionId));
        }

        var winner = _reservations[clientOrderId];
        return Task.FromResult(new ClientOrderIdReservationResult(Reserved: false, ExistingOrderId: winner.OrderId, ExistingDecisionId: winner.DecisionId));
    }
}
