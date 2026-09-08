using System.Security.Cryptography;
using System.Text;

namespace TradingPulse.Infrastructure.Security;

/// <summary>
/// Pure key-material helpers for <c>EfApiKeyRepository</c> - generation and hashing, with no
/// EF/database dependency, so this piece of the per-client-key logic can be built and exercised
/// on its own (see the project's standard "EF-stash" verification approach in the README).
/// </summary>
public static class ApiKeyGenerator
{
    private const string Prefix = "tpk_";

    /// <summary>
    /// Generates a new random raw API key: a fixed "tpk_" prefix (mirrors the Stripe/GitHub
    /// convention of a recognizable prefix, useful for secret-scanning and at-a-glance
    /// identification in logs) followed by 256 bits of randomness, URL-safe base64 encoded.
    /// </summary>
    public static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return Prefix + token;
    }

    /// <summary>
    /// SHA-256 hash of the raw key, as lowercase hex. Only this ever gets persisted - the raw
    /// key exists in memory for the one request that mints it and nowhere else. No per-key salt:
    /// unlike a password, an API key is already 256 bits of server-generated randomness, so a
    /// rainbow-table attack isn't a meaningful threat model here (see README Design Decisions).
    /// </summary>
    public static string Hash(string rawKey) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

    /// <summary>Constant-time string comparison, for comparing a caller-supplied key against the configured master key without leaking timing information.</summary>
    public static bool ConstantTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return aBytes.Length == bBytes.Length && CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
