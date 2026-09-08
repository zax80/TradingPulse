namespace TradingPulse.Api.Contracts;

public sealed record CreateApiKeyRequest(string ClientName);

/// <summary>The raw key is included exactly once, in this response - it is never retrievable again.</summary>
public sealed record CreateApiKeyResponse(string ClientName, string ApiKey);
