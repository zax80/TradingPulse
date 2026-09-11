namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>
/// One row per ClientOrderId ever claimed - the source of truth for "who got here first" under
/// concurrent submissions. Deliberately separate from <see cref="OrderEntity"/>, which keeps a full
/// audit row for every attempt (including rejected duplicates) and is therefore not itself unique on
/// ClientOrderId. The primary key here - the actual uniqueness guarantee - is the normalized id
/// itself, not a surrogate <c>Guid</c>. See <c>EfClientOrderIdReservationStore</c>.
/// </summary>
public sealed class ClientOrderIdReservationEntity
{
    /// <summary>Lowercased ClientOrderId - matches the case-insensitive comparison
    /// <see cref="Repositories.EfOrderRepository.GetByClientOrderIdAsync"/> already uses.</summary>
    public required string ClientOrderIdNormalized { get; set; }

    public Guid OrderId { get; set; }
    public Guid DecisionId { get; set; }
    public DateTimeOffset ReservedAt { get; set; }
}
