using System.Text.Json;
using System.Text.Json.Serialization;
using TradingPulse.Domain;
using TradingPulse.Domain.Enums;

namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>Persistence model for <see cref="OrderDecision"/>. Rejection reasons are stored as a JSON array string - no fixed count, no separate table needed.</summary>
public sealed class OrderDecisionEntity
{
    // Enum stored as its name (not the numeric value) so the JSON column stays readable if
    // inspected directly in the database - matches the API's global JsonStringEnumConverter.
    private static readonly JsonSerializerOptions ReasonsJsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

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
        RejectionReasonsJson = JsonSerializer.Serialize(decision.RejectionReasons, ReasonsJsonOptions),
        DecidedAt = decision.DecidedAt,
    };

    public OrderDecision ToDomain() => OrderDecision.Rehydrate(
        Id,
        OrderId,
        Enum.Parse<DecisionStatus>(Status),
        JsonSerializer.Deserialize<List<RejectionReason>>(RejectionReasonsJson, ReasonsJsonOptions) ?? [],
        DecidedAt);
}
