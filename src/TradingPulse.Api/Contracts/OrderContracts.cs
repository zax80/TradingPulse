namespace TradingPulse.Api.Contracts;

/// <summary>Request body for <c>POST /api/orders</c>.</summary>
public sealed record SubmitOrderRequest(
    string ClientOrderId,
    string Symbol,
    string Side,
    string Type,
    decimal Price,
    decimal Quantity);

/// <summary>Response for <c>POST /api/orders</c>: the decision, with just enough order context to identify it.</summary>
public sealed record OrderSubmissionResponse(
    Guid OrderId,
    string ClientOrderId,
    string Symbol,
    string Side,
    string Status,
    IReadOnlyList<string> RejectionReasons);
