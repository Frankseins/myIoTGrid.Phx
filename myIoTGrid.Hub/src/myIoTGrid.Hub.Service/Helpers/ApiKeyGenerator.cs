using System.Security.Cryptography;
using System.Text;

namespace myIoTGrid.Hub.Service.Helpers;

/// <summary>
/// Helper class for generating and validating API keys.
/// Format: mig_key_{32 random chars}
/// </summary>
public static class ApiKeyGenerator
{
    private const string Prefix = "mig_key_";
    private const int RandomPartLength = 32;
    private const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Generates a new API key with format: mig_key_{32 random chars}
    /// </summary>
    public static string GenerateApiKey()
    {
        var randomPart = new StringBuilder(RandomPartLength);
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[RandomPartLength];
        rng.GetBytes(bytes);

        foreach (var b in bytes)
        {
            randomPart.Append(AllowedChars[b % AllowedChars.Length]);
        }

        return $"{Prefix}{randomPart}";
    }

    /// <summary>
    /// Hashes an API key using SHA256.
    /// The hash is stored in the database, not the plain key.
    /// </summary>
    public static string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Validates an API key format.
    /// </summary>
    public static bool IsValidFormat(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        if (!apiKey.StartsWith(Prefix))
            return false;

        var randomPart = apiKey[Prefix.Length..];
        if (randomPart.Length != RandomPartLength)
            return false;

        return randomPart.All(c => AllowedChars.Contains(c));
    }

    /// <summary>
    /// Validates an API key against a stored hash.
    /// </summary>
    public static bool ValidateApiKey(string apiKey, string storedHash)
    {
        if (!IsValidFormat(apiKey))
            return false;

        var keyHash = HashApiKey(apiKey);
        return string.Equals(keyHash, storedHash, StringComparison.OrdinalIgnoreCase);
    }
}
