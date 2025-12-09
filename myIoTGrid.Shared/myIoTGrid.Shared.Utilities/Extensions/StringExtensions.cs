using System.Text.RegularExpressions;

namespace myIoTGrid.Shared.Utilities.Extensions;

/// <summary>
/// Extension methods for string operations
/// </summary>
public static partial class StringExtensions
{
    /// <summary>
    /// Checks if a string is null or whitespace
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if null or whitespace</returns>
    public static bool IsNullOrWhiteSpace(this string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Checks if a string has actual content (not null or whitespace)
    /// </summary>
    /// <param name="value">The string to check</param>
    /// <returns>True if has content</returns>
    public static bool HasContent(this string? value)
        => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Truncates a string to a maximum length
    /// </summary>
    /// <param name="value">The string to truncate</param>
    /// <param name="maxLength">Maximum length</param>
    /// <param name="suffix">Suffix to add if truncated (default "...")</param>
    /// <returns>Truncated string</returns>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return string.Concat(value.AsSpan(0, maxLength - suffix.Length), suffix);
    }

    /// <summary>
    /// Converts a string to a URL-safe slug
    /// </summary>
    /// <param name="value">The string to convert</param>
    /// <returns>URL-safe slug</returns>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Convert to lowercase and replace spaces with hyphens
        var slug = value.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue")
            .Replace("ß", "ss");

        // Remove invalid characters
        slug = SlugRegex().Replace(slug, "");

        // Remove duplicate hyphens
        slug = DuplicateHyphenRegex().Replace(slug, "-");

        return slug.Trim('-');
    }

    /// <summary>
    /// Validates if a string is a valid MAC address
    /// </summary>
    /// <param name="value">The string to validate</param>
    /// <returns>True if valid MAC address</returns>
    public static bool IsValidMacAddress(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return MacAddressRegex().IsMatch(value);
    }

    /// <summary>
    /// Normalizes a MAC address to uppercase with colons
    /// </summary>
    /// <param name="value">The MAC address to normalize</param>
    /// <returns>Normalized MAC address or original if invalid</returns>
    public static string NormalizeMacAddress(this string value)
    {
        if (!value.IsValidMacAddress())
            return value;

        // Remove all separators and convert to uppercase
        var clean = value.Replace(":", "").Replace("-", "").ToUpperInvariant();

        // Insert colons
        return string.Join(":",
            Enumerable.Range(0, 6).Select(i => clean.Substring(i * 2, 2)));
    }

    /// <summary>
    /// Masks sensitive data (shows first and last N characters)
    /// </summary>
    /// <param name="value">The string to mask</param>
    /// <param name="visibleChars">Number of visible characters at start and end</param>
    /// <returns>Masked string</returns>
    public static string Mask(this string? value, int visibleChars = 3)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars * 2)
            return value ?? string.Empty;

        var start = value[..visibleChars];
        var end = value[^visibleChars..];
        var maskLength = value.Length - (visibleChars * 2);

        return $"{start}{new string('*', Math.Min(maskLength, 10))}{end}";
    }

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex("-+")]
    private static partial Regex DuplicateHyphenRegex();

    [GeneratedRegex("^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$|^([0-9A-Fa-f]{12})$")]
    private static partial Regex MacAddressRegex();
}
