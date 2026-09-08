namespace TradingPulse.Domain.Enums;

/// <summary>
/// Machine-readable code for why an order was rejected. Lets a client branch
/// on the reason (e.g. retry after a price-deviation rejection, don't retry
/// after a whitelist rejection) without parsing the human-readable message.
/// </summary>
public enum RejectionReasonCode
{
    MaxNotionalExceeded = 0,
    MaxQuantityExceeded = 1,
    PriceDeviationExceeded = 2,
    NoCurrentPrice = 3,
    DuplicateClientOrderId = 4,
    SymbolNotWhitelisted = 5,
}
