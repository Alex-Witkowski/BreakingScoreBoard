using System.Security.Cryptography;
using System.Text;

namespace BreakingScoreBoard.Api.Infrastructure;

/// <summary>
/// Utility for hashing PINs using SHA256.
/// </summary>
public static class PinHasher
{
    /// <summary>
    /// Hashes a PIN string using SHA256.
    /// </summary>
    /// <param name="pin">The PIN to hash.</param>
    /// <returns>Hexadecimal string representation of the hash.</returns>
    public static string Hash(string pin)
    {
        if (string.IsNullOrEmpty(pin))
        {
            throw new ArgumentException("PIN cannot be null or empty", nameof(pin));
        }

        var bytes = Encoding.UTF8.GetBytes(pin);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies a PIN against a stored hash.
    /// </summary>
    /// <param name="pin">The PIN to verify.</param>
    /// <param name="hash">The stored hash to compare against.</param>
    /// <returns>True if the PIN matches the hash.</returns>
    public static bool Verify(string pin, string hash)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        var computedHash = Hash(pin);
        return string.Equals(computedHash, hash, StringComparison.OrdinalIgnoreCase);
    }
}
