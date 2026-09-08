using TradingPulse.Domain.Enums;

namespace TradingPulse.Domain;

/// <summary>Outcome of evaluating an <see cref="Order"/> against the current <see cref="TradingRules"/>.</summary>
public sealed class OrderDecision
{
    public Guid Id { get; }
    public Guid OrderId { get; }
    public DecisionStatus Status { get; }
    public IReadOnlyList<RejectionReason> RejectionReasons { get; }
    public DateTimeOffset DecidedAt { get; }

    private OrderDecision(
        Guid id,
        Guid orderId,
        DecisionStatus status,
        IReadOnlyList<RejectionReason> rejectionReasons,
        DateTimeOffset decidedAt)
    {
        Id = id;
        OrderId = orderId;
        Status = status;
        RejectionReasons = rejectionReasons;
        DecidedAt = decidedAt;
    }

    public static OrderDecision Accept(Guid orderId, DateTimeOffset decidedAt) =>
        new(Guid.NewGuid(), orderId, DecisionStatus.Accepted, Array.Empty<RejectionReason>(), decidedAt);

    public static OrderDecision Reject(Guid orderId, IReadOnlyList<RejectionReason> reasons, DateTimeOffset decidedAt)
    {
        if (reasons.Count == 0)
        {
            throw new ArgumentException("A rejected decision must have at least one reason.", nameof(reasons));
        }

        return new(Guid.NewGuid(), orderId, DecisionStatus.Rejected, reasons, decidedAt);
    }

    /// <summary>Reconstructs a decision from persisted state, preserving its original id. Not for deciding new orders - use Accept/Reject for that.</summary>
    public static OrderDecision Rehydrate(Guid id, Guid orderId, DecisionStatus status, IReadOnlyList<RejectionReason> rejectionReasons, DateTimeOffset decidedAt) =>
        new(id, orderId, status, rejectionReasons, decidedAt);
}
