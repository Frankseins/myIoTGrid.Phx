namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO for Hub (Raspberry Pi Gateway) information
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="TenantId">Tenant-ID</param>
/// <param name="HubId">Hub identifier (e.g., "hub-home-01")</param>
/// <param name="Name">Display name</param>
/// <param name="Description">Optional description</param>
/// <param name="LastSeen">Last heartbeat</param>
/// <param name="IsOnline">Online status</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="SensorCount">Number of connected sensors</param>
public record HubDto(
    Guid Id,
    Guid TenantId,
    string HubId,
    string Name,
    string? Description,
    DateTime? LastSeen,
    bool IsOnline,
    DateTime CreatedAt,
    int SensorCount
);

/// <summary>
/// DTO for creating a Hub
/// </summary>
/// <param name="HubId">Hub identifier (e.g., "hub-home-01")</param>
/// <param name="Name">Display name (optional, will be generated from HubId)</param>
/// <param name="Description">Optional description</param>
public record CreateHubDto(
    string HubId,
    string? Name = null,
    string? Description = null
);

/// <summary>
/// DTO for updating a Hub
/// </summary>
/// <param name="Name">New display name</param>
/// <param name="Description">New description</param>
public record UpdateHubDto(
    string? Name = null,
    string? Description = null
);
