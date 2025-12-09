namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Storage mode for sensor readings (Domain)
/// Sprint OS-01: Offline Storage
/// </summary>
public enum StorageMode
{
    /// <summary>Only send readings to Hub, no local storage</summary>
    RemoteOnly = 0,

    /// <summary>Store locally and send to Hub</summary>
    LocalAndRemote = 1,

    /// <summary>Only store locally, no network transmission</summary>
    LocalOnly = 2,

    /// <summary>Store locally and auto-sync when connection available</summary>
    LocalAutoSync = 3
}
