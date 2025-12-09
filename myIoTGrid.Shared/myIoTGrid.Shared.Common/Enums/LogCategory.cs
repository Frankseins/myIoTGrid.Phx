namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Log category for node debug logs (Domain)
/// Sprint 8: Remote Debug System
/// </summary>
public enum LogCategory
{
    /// <summary>General system messages</summary>
    System = 0,

    /// <summary>WiFi connection</summary>
    WiFi = 1,

    /// <summary>Sensor readings</summary>
    Sensor = 2,

    /// <summary>HTTP communication</summary>
    Http = 3,

    /// <summary>Storage operations</summary>
    Storage = 4,

    /// <summary>Power management</summary>
    Power = 5,

    /// <summary>Configuration</summary>
    Config = 6,

    /// <summary>OTA updates</summary>
    Ota = 7,

    /// <summary>Hardware-related messages</summary>
    Hardware = 8,

    /// <summary>Network-related messages</summary>
    Network = 9,

    /// <summary>GPS-related messages</summary>
    GPS = 10,

    /// <summary>API-related messages</summary>
    API = 11,

    /// <summary>Error messages</summary>
    Error = 12
}
