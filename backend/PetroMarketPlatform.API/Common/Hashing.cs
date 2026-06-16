using System.Security.Cryptography;
using System.Text;

namespace PetroMarketPlatform.API.Common;

public static class Hashing
{
    /// <summary>Deterministic SHA-256 hex — used for OTP codes and refresh tokens at rest.</summary>
    public static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>URL-safe cryptographically-random token.</summary>
    public static string RandomToken(int bytes = 32)
    {
        var buffer = RandomNumberGenerator.GetBytes(bytes);
        return Convert.ToBase64String(buffer)
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    /// <summary>Numeric OTP of the given length (zero-padded).</summary>
    public static string NumericCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        var n = RandomNumberGenerator.GetInt32(0, max);
        return n.ToString(new string('0', length));
    }
}
