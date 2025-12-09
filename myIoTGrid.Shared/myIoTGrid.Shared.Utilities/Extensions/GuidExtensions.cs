namespace myIoTGrid.Shared.Utilities.Extensions;

/// <summary>
/// Extension methods for Guid operations
/// </summary>
public static class GuidExtensions
{
    /// <summary>
    /// Checks if a Guid is empty (Guid.Empty)
    /// </summary>
    /// <param name="guid">The Guid to check</param>
    /// <returns>True if empty</returns>
    public static bool IsEmpty(this Guid guid)
        => guid == Guid.Empty;

    /// <summary>
    /// Checks if a Guid is not empty
    /// </summary>
    /// <param name="guid">The Guid to check</param>
    /// <returns>True if not empty</returns>
    public static bool IsNotEmpty(this Guid guid)
        => guid != Guid.Empty;

    /// <summary>
    /// Checks if a nullable Guid has a non-empty value
    /// </summary>
    /// <param name="guid">The nullable Guid to check</param>
    /// <returns>True if has a non-empty value</returns>
    public static bool HasValue(this Guid? guid)
        => guid.HasValue && guid.Value != Guid.Empty;

    /// <summary>
    /// Gets a short representation of the Guid (first 8 characters)
    /// </summary>
    /// <param name="guid">The Guid</param>
    /// <returns>Short string representation</returns>
    public static string ToShortString(this Guid guid)
        => guid.ToString("N")[..8];

    /// <summary>
    /// Tries to parse a string to a Guid, returning null if invalid
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <returns>Parsed Guid or null</returns>
    public static Guid? TryParseGuid(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(value, out var guid) ? guid : null;
    }

    /// <summary>
    /// Parses a string to Guid or returns Guid.Empty if invalid
    /// </summary>
    /// <param name="value">The string to parse</param>
    /// <returns>Parsed Guid or Guid.Empty</returns>
    public static Guid ParseOrEmpty(this string? value)
        => value.TryParseGuid() ?? Guid.Empty;
}
