using TradingPulse.Domain.Enums;

namespace TradingPulse.Domain;

/// <summary>
/// One rule violation: a <see cref="RejectionReasonCode"/> a client can branch on,
/// plus a human-readable message for logs/UI.
/// </summary>
public sealed record RejectionReason(RejectionReasonCode Code, string Message);
