
namespace myIoTGrid.Hub.Service.Extensions;

/// <summary>
/// Mapping Extensions for Node Entity.
/// </summary>
public static class NodeMappingExtensions
{
    /// <summary>
    /// Converts a Node entity to a NodeDto
    /// </summary>
    public static NodeDto ToDto(this Node node)
    {
        return new NodeDto(
            Id: node.Id,
            HubId: node.HubId,
            NodeId: node.NodeId,
            Name: node.Name,
            Protocol: node.Protocol.ToDto(),
            Location: node.Location?.ToDto(),
            AssignmentCount: node.SensorAssignments.Count,
            LastSeen: node.LastSeen,
            IsOnline: node.IsOnline,
            FirmwareVersion: node.FirmwareVersion,
            BatteryLevel: node.BatteryLevel,
            CreatedAt: node.CreatedAt,
            MacAddress: node.MacAddress,
            Status: node.Status.ToDto(),
            IsSimulation: node.IsSimulation,
            // Sprint OS-01: Offline Storage
            StorageMode: node.StorageMode.ToDto(),
            PendingSyncCount: node.PendingSyncCount,
            LastSyncAt: node.LastSyncAt,
            LastSyncError: node.LastSyncError,
            // Sprint 8: Remote Debug System
            DebugLevel: node.DebugLevel.ToDto(),
            EnableRemoteLogging: node.EnableRemoteLogging,
            LastDebugChange: node.LastDebugChange
        );
    }

    /// <summary>
    /// Converts DebugLevel enum to DebugLevelDto
    /// </summary>
    public static DebugLevelDto ToDto(this DebugLevel level)
    {
        return level switch
        {
            DebugLevel.Production => DebugLevelDto.Production,
            DebugLevel.Normal => DebugLevelDto.Normal,
            DebugLevel.Debug => DebugLevelDto.Debug,
            _ => DebugLevelDto.Normal
        };
    }

    /// <summary>
    /// Converts DebugLevelDto to DebugLevel enum
    /// </summary>
    public static DebugLevel ToEntity(this DebugLevelDto level)
    {
        return level switch
        {
            DebugLevelDto.Production => DebugLevel.Production,
            DebugLevelDto.Normal => DebugLevel.Normal,
            DebugLevelDto.Debug => DebugLevel.Debug,
            _ => DebugLevel.Normal
        };
    }

    /// <summary>
    /// Converts NodeStatus enum to NodeProvisioningStatusDto
    /// </summary>
    public static NodeProvisioningStatusDto ToDto(this NodeStatus status)
    {
        return status switch
        {
            NodeStatus.Unconfigured => NodeProvisioningStatusDto.Unconfigured,
            NodeStatus.Pairing => NodeProvisioningStatusDto.Pairing,
            NodeStatus.Configured => NodeProvisioningStatusDto.Configured,
            NodeStatus.Error => NodeProvisioningStatusDto.Error,
            _ => NodeProvisioningStatusDto.Unconfigured
        };
    }

    /// <summary>
    /// Converts NodeProvisioningStatusDto to NodeStatus enum
    /// </summary>
    public static NodeStatus ToEntity(this NodeProvisioningStatusDto status)
    {
        return status switch
        {
            NodeProvisioningStatusDto.Unconfigured => NodeStatus.Unconfigured,
            NodeProvisioningStatusDto.Pairing => NodeStatus.Pairing,
            NodeProvisioningStatusDto.Configured => NodeStatus.Configured,
            NodeProvisioningStatusDto.Error => NodeStatus.Error,
            _ => NodeStatus.Unconfigured
        };
    }

    /// <summary>
    /// Converts a CreateNodeDto to a Node entity
    /// </summary>
    public static Node ToEntity(this CreateNodeDto dto, Guid hubId)
    {
        return new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = dto.NodeId,
            Name = dto.Name ?? GenerateNameFromNodeId(dto.NodeId),
            Protocol = dto.Protocol.ToEntity(),
            Location = dto.Location?.ToEntity(),
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MacAddress = GenerateMacFromNodeId(dto.NodeId), // Generate MAC from NodeId for simulated nodes
            ApiKeyHash = string.Empty, // Will be set during provisioning
            Status = NodeStatus.Configured // Nodes created via API/wizard are configured
        };
    }

    /// <summary>
    /// Generates a pseudo-MAC address from NodeId for simulated/non-BLE registered nodes
    /// </summary>
    private static string GenerateMacFromNodeId(string nodeId)
    {
        // Create a hash-based MAC address from the nodeId
        // Format: SIM-XX:XX:XX:XX:XX:XX where XX are hex digits derived from nodeId
        var hash = nodeId.GetHashCode();
        var bytes = BitConverter.GetBytes(hash);
        var extraBytes = BitConverter.GetBytes(nodeId.Length + nodeId.Sum(c => c));

        return $"SIM-{bytes[0]:X2}:{bytes[1]:X2}:{bytes[2]:X2}:{bytes[3]:X2}:{extraBytes[0]:X2}:{extraBytes[1]:X2}";
    }

    /// <summary>
    /// Converts a NodeRegistrationDto to a Node entity for provisioning
    /// </summary>
    public static Node ToEntity(this NodeRegistrationDto dto, Guid hubId, string apiKeyHash)
    {
        return new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = GenerateNodeIdFromMac(dto.MacAddress),
            Name = dto.Name ?? GenerateNameFromMac(dto.MacAddress),
            Protocol = Protocol.WLAN,
            MacAddress = dto.MacAddress.ToUpperInvariant(),
            ApiKeyHash = apiKeyHash,
            FirmwareVersion = dto.FirmwareVersion,
            Status = NodeStatus.Configured,
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generates a NodeId from MAC address
    /// </summary>
    private static string GenerateNodeIdFromMac(string macAddress)
    {
        // Remove colons and convert to lowercase: "AA:BB:CC:DD:EE:FF" -> "node-aabbccddeeff"
        var cleanMac = macAddress.Replace(":", "").ToLowerInvariant();
        return $"node-{cleanMac}";
    }

    /// <summary>
    /// Generates a display name from MAC address
    /// </summary>
    private static string GenerateNameFromMac(string macAddress)
    {
        // Take last 4 chars of MAC: "AA:BB:CC:DD:EE:FF" -> "Node EEFF"
        var cleanMac = macAddress.Replace(":", "").ToUpperInvariant();
        var suffix = cleanMac.Length >= 4 ? cleanMac[^4..] : cleanMac;
        return $"Node {suffix}";
    }

    /// <summary>
    /// Applies an UpdateNodeDto to a Node entity
    /// </summary>
    public static void ApplyUpdate(this Node node, UpdateNodeDto dto)
    {
        if (!string.IsNullOrEmpty(dto.Name))
            node.Name = dto.Name;

        if (dto.Location != null)
            node.Location = dto.Location.ToEntity();

        if (!string.IsNullOrEmpty(dto.FirmwareVersion))
            node.FirmwareVersion = dto.FirmwareVersion;

        if (dto.IsSimulation.HasValue)
            node.IsSimulation = dto.IsSimulation.Value;

        // Sprint OS-01: Update StorageMode
        if (dto.StorageMode.HasValue)
            node.StorageMode = dto.StorageMode.Value.ToEntity();
    }

    /// <summary>
    /// Applies a NodeStatusDto to a Node entity
    /// </summary>
    public static void ApplyStatus(this Node node, NodeStatusDto status)
    {
        node.IsOnline = status.IsOnline;

        if (status.LastSeen.HasValue)
            node.LastSeen = status.LastSeen;

        if (status.BatteryLevel.HasValue)
            node.BatteryLevel = status.BatteryLevel;
    }

    private static string GenerateNameFromNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return "Unknown Node";

        var parts = nodeId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }
}
