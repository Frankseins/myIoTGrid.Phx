namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Debug level for node logging (Domain)
/// Sprint 8: Remote Debug System
/// Values must match DebugLevelDto for direct mapping.
/// Lower values = less verbose (production), higher values = more verbose (debug).
/// </summary>
public enum DebugLevel
{
    /// <summary>Production mode (minimal logging, errors only)</summary>
    Production = 0,

    /// <summary>Normal operation logging</summary>
    Normal = 1,

    /// <summary>Verbose logging for debugging</summary>
    Debug = 2
}
