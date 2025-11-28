using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Shared.DTOs;

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
            Sensors: node.Sensors.Select(s => s.ToDto()),
            LastSeen: node.LastSeen,
            IsOnline: node.IsOnline,
            FirmwareVersion: node.FirmwareVersion,
            BatteryLevel: node.BatteryLevel,
            CreatedAt: node.CreatedAt
        );
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
            CreatedAt = DateTime.UtcNow
        };
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
