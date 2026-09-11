using System.Security.Cryptography;
using System.Text;

namespace AniRankApp.Helpers;

/// <summary>Small password hashing helper (SHA-256, hex encoded).</summary>
public static class SecurityHelper
{
    public static string Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input ?? string.Empty));
        return Convert.ToHexString(bytes);
    }
}
