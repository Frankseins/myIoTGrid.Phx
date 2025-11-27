using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO für Alert-Typ Informationen
/// </summary>
/// <param name="Id">Primärschlüssel</param>
/// <param name="Code">Code (z.B. "mold_risk")</param>
/// <param name="Name">Anzeigename (z.B. "Schimmelrisiko")</param>
/// <param name="Description">Beschreibung</param>
/// <param name="DefaultLevel">Standard-Warnstufe</param>
/// <param name="IconName">Material Icon Name</param>
/// <param name="IsGlobal">Ob global (von Cloud definiert)</param>
/// <param name="CreatedAt">Erstellungszeitpunkt</param>
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
/// DTO zum Erstellen eines Alert-Typs
/// </summary>
/// <param name="Code">Code (z.B. "mold_risk")</param>
/// <param name="Name">Anzeigename (z.B. "Schimmelrisiko")</param>
/// <param name="Description">Beschreibung</param>
/// <param name="DefaultLevel">Standard-Warnstufe</param>
/// <param name="IconName">Material Icon Name</param>
public record CreateAlertTypeDto(
    string Code,
    string Name,
    string? Description = null,
    AlertLevelDto DefaultLevel = AlertLevelDto.Warning,
    string? IconName = null
);
