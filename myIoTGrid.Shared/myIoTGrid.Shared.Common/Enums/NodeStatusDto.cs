namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// DTO enum for Node provisioning status.
/// </summary>
public enum NodeProvisioningStatusDto
{
    /// <summary>Node is not yet configured (initial BLE pairing state)</summary>
    Unconfigured = 0,

    /// <summary>Node is currently in BLE pairing mode</summary>
    Pairing = 1,

    /// <summary>Node is fully configured and operational</summary>
    Configured = 2,

    /// <summary>Node is in error state</summary>
    Error = 3
}
