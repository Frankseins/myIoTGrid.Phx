namespace myIoTGrid.Shared.Utilities.Extensions;

/// <summary>
/// Extension methods for DateTime operations
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts DateTime to ISO 8601 format string
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>ISO 8601 formatted string</returns>
    public static string ToIso8601(this DateTime dateTime)
        => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    /// <summary>
    /// Converts a Unix timestamp (seconds) to DateTime
    /// </summary>
    /// <param name="timestamp">Unix timestamp in seconds</param>
    /// <returns>UTC DateTime</returns>
    public static DateTime FromUnixTimestamp(long timestamp)
        => DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;

    /// <summary>
    /// Converts a Unix timestamp (milliseconds) to DateTime
    /// </summary>
    /// <param name="timestampMs">Unix timestamp in milliseconds</param>
    /// <returns>UTC DateTime</returns>
    public static DateTime FromUnixTimestampMs(long timestampMs)
        => DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime;

    /// <summary>
    /// Converts DateTime to Unix timestamp (seconds)
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>Unix timestamp in seconds</returns>
    public static long ToUnixTimestamp(this DateTime dateTime)
        => new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();

    /// <summary>
    /// Converts DateTime to Unix timestamp (milliseconds)
    /// </summary>
    /// <param name="dateTime">The DateTime to convert</param>
    /// <returns>Unix timestamp in milliseconds</returns>
    public static long ToUnixTimestampMs(this DateTime dateTime)
        => new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();

    /// <summary>
    /// Gets the start of day (00:00:00) for a given DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>DateTime at start of day</returns>
    public static DateTime StartOfDay(this DateTime dateTime)
        => dateTime.Date;

    /// <summary>
    /// Gets the end of day (23:59:59.999) for a given DateTime
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>DateTime at end of day</returns>
    public static DateTime EndOfDay(this DateTime dateTime)
        => dateTime.Date.AddDays(1).AddTicks(-1);

    /// <summary>
    /// Checks if a DateTime is within a specified range
    /// </summary>
    /// <param name="dateTime">The DateTime to check</param>
    /// <param name="start">Range start (inclusive)</param>
    /// <param name="end">Range end (inclusive)</param>
    /// <returns>True if within range</returns>
    public static bool IsWithinRange(this DateTime dateTime, DateTime start, DateTime end)
        => dateTime >= start && dateTime <= end;

    /// <summary>
    /// Gets a human-readable relative time string (e.g., "2 hours ago")
    /// </summary>
    /// <param name="dateTime">The DateTime</param>
    /// <returns>Relative time string</returns>
    public static string ToRelativeTimeString(this DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalSeconds < 60)
            return "gerade eben";
        if (diff.TotalMinutes < 60)
            return $"vor {(int)diff.TotalMinutes} Minuten";
        if (diff.TotalHours < 24)
            return $"vor {(int)diff.TotalHours} Stunden";
        if (diff.TotalDays < 30)
            return $"vor {(int)diff.TotalDays} Tagen";
        if (diff.TotalDays < 365)
            return $"vor {(int)(diff.TotalDays / 30)} Monaten";

        return $"vor {(int)(diff.TotalDays / 365)} Jahren";
    }
}
