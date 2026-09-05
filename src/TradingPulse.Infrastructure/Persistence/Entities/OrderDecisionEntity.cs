using System.Text.Json;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;

namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>Persistence model for <see cref="OrderDecision"/>. Rejection reasons are stored as a JSON array string - no fixed count, no separate table needed.</summary>
public sealed class OrderDecisionEntity
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string Status { get; set; }
    public required string RejectionReasonsJson { get; set; }
    public DateTimeOffset DecidedAt { get; set; }

    public static OrderDecisionEntity FromDomain(OrderDecision decision) => new()
    {
        Id = decision.Id,
        OrderId = decision.OrderId,
        Status = decision.Status.ToString(),
        RejectionReasonsJson = JsonSerializer.Serialize(decision.RejectionReasons),
        DecidedAt = decision.DecidedAt,
    };

    public OrderDecision ToDomain() => OrderDecision.Rehydrate(
        Id,
        OrderId,
        Enum.Parse<DecisionStatus>(Status),
        JsonSerializer.Deserialize<List<string>>(RejectionReasonsJson) ?? [],
        DecidedAt);
}
