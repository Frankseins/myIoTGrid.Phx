using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.DTOs;

/// <summary>
/// DTO for AlertType information
/// </summary>
public record AlertTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    AlertLevelDto DefaultLevel,
    string? IconName,
    bool IsGlobal,
    DateTime CreatedAt
);

/// <summary>
/// DTO for creating an AlertType
/// </summary>
public record CreateAlertTypeDto(
    string Code,
    string Name,
    string? Description = null,
    AlertLevelDto DefaultLevel = AlertLevelDto.Info,
    string? IconName = null
);

/// <summary>
/// DTO for updating an AlertType
/// </summary>
public record UpdateAlertTypeDto(
    string? Name = null,
    string? Description = null,
    AlertLevelDto? DefaultLevel = null,
    string? IconName = null
);
