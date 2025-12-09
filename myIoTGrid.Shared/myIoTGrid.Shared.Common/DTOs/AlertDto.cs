using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for Alert information
/// </summary>
/// <param name="Id">Primary key</param>
/// <param name="TenantId">Tenant-ID</param>
/// <param name="HubId">Affected Hub (optional)</param>
/// <param name="HubName">Hub display name</param>
/// <param name="NodeId">Affected Node (optional)</param>
/// <param name="NodeName">Node display name</param>
/// <param name="AlertTypeId">Alert type ID</param>
/// <param name="AlertTypeCode">Alert type code</param>
/// <param name="AlertTypeName">Alert type display name</param>
/// <param name="Level">Alert level</param>
/// <param name="Message">Alert message</param>
/// <param name="Recommendation">Recommendation for resolution</param>
/// <param name="Source">Source (local or Cloud)</param>
/// <param name="CreatedAt">Creation timestamp</param>
/// <param name="ExpiresAt">Expiration timestamp</param>
/// <param name="AcknowledgedAt">Acknowledgment timestamp</param>
/// <param name="IsActive">Whether active</param>
public record AlertDto(
    Guid Id,
    Guid TenantId,
    Guid? HubId,
    string? HubName,
    Guid? NodeId,
    string? NodeName,
    Guid AlertTypeId,
    string AlertTypeCode,
    string AlertTypeName,
    AlertLevelDto Level,
    string Message,
    string? Recommendation,
    AlertSourceDto Source,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? AcknowledgedAt,
    bool IsActive
);

/// <summary>
/// DTO for creating an Alert (received from Cloud)
/// </summary>
/// <param name="AlertTypeCode">Alert type code (e.g., "mold_risk")</param>
/// <param name="HubId">Affected Hub identifier (optional)</param>
/// <param name="NodeId">Affected Node identifier (optional)</param>
/// <param name="Level">Alert level</param>
/// <param name="Message">Alert message</param>
/// <param name="Recommendation">Recommendation for resolution</param>
/// <param name="ExpiresAt">Expiration timestamp</param>
public record CreateAlertDto(
    string AlertTypeCode,
    string? HubId = null,
    string? NodeId = null,
    AlertLevelDto Level = AlertLevelDto.Warning,
    string Message = "",
    string? Recommendation = null,
    DateTime? ExpiresAt = null
);

/// <summary>
/// DTO for acknowledging an Alert
/// </summary>
/// <param name="AlertId">Alert ID</param>
public record AcknowledgeAlertDto(
    Guid AlertId
);

/// <summary>
/// DTO for filtering Alerts
/// </summary>
/// <param name="HubId">Filter by Hub (Guid)</param>
/// <param name="NodeId">Filter by Node (Guid)</param>
/// <param name="AlertTypeCode">Filter by alert type code</param>
/// <param name="Level">Filter by level</param>
/// <param name="Source">Filter by source</param>
/// <param name="IsActive">Filter by active status</param>
/// <param name="IsAcknowledged">Filter by acknowledgment status</param>
/// <param name="From">Time range start</param>
/// <param name="To">Time range end</param>
/// <param name="Page">Page number (1-based)</param>
/// <param name="PageSize">Items per page</param>
public record AlertFilterDto(
    Guid? HubId = null,
    Guid? NodeId = null,
    string? AlertTypeCode = null,
    AlertLevelDto? Level = null,
    AlertSourceDto? Source = null,
    bool? IsActive = null,
    bool? IsAcknowledged = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50
);
