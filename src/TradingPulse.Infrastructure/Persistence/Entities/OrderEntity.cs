using TradingPulse.Domain;
using TradingPulse.Domain.Enums;

namespace TradingPulse.Infrastructure.Persistence.Entities;

/// <summary>Persistence model for <see cref="Order"/>. Enums are stored as strings for a readable schema.</summary>
public sealed class OrderEntity
{
    public Guid Id { get; set; }
    public required string ClientOrderId { get; set; }
    public required string Symbol { get; set; }
    public required string Side { get; set; }
    public required string Type { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public required string Origin { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }

    public static OrderEntity FromDomain(Order order) => new()
    {
        Id = order.Id,
        ClientOrderId = order.ClientOrderId,
        Symbol = order.Symbol,
        Side = order.Side.ToString(),
        Type = order.Type.ToString(),
        Price = order.Price,
        Quantity = order.Quantity,
        Origin = order.Origin.ToString(),
        SubmittedAt = order.SubmittedAt,
    };

    public Order ToDomain() => Order.Rehydrate(
        Id,
        ClientOrderId,
        Symbol,
        Enum.Parse<OrderSide>(Side),
        Enum.Parse<OrderType>(Type),
        Price,
        Quantity,
        Enum.Parse<OrderOrigin>(Origin),
        SubmittedAt);
}
