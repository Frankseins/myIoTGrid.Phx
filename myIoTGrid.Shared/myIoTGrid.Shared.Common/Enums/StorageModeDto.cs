namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// DTO enum for storage mode configuration.
/// Part of Sprint OS-01: Offline-Speicher Implementation.
/// </summary>
public enum StorageModeDto
{
    /// <summary>
    /// Only send to Hub, no local storage (default)
    /// </summary>
    RemoteOnly = 0,

    /// <summary>
    /// Store locally AND send to Hub simultaneously
    /// </summary>
    LocalAndRemote = 1,

    /// <summary>
    /// Only store locally, never send to Hub
    /// </summary>
    LocalOnly = 2,

    /// <summary>
    /// Store locally, auto-sync when WiFi available
    /// </summary>
    LocalAutoSync = 3
}
