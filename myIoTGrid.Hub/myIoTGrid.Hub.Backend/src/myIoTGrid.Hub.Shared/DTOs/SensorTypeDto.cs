namespace myIoTGrid.Hub.Shared.DTOs;

/// <summary>
/// DTO für Sensor-Typ Informationen
/// </summary>
/// <param name="Id">Primärschlüssel</param>
/// <param name="Code">Code (z.B. "temperature")</param>
/// <param name="Name">Anzeigename (z.B. "Temperatur")</param>
/// <param name="Unit">Einheit (z.B. "°C")</param>
/// <param name="Description">Beschreibung</param>
/// <param name="IconName">Material Icon Name</param>
/// <param name="IsGlobal">Ob global (von Cloud definiert)</param>
/// <param name="CreatedAt">Erstellungszeitpunkt</param>
public record SensorTypeDto(
    Guid Id,
    string Code,
    string Name,
    string Unit,
    string? Description,
    string? IconName,
    bool IsGlobal,
    DateTime CreatedAt
);

/// <summary>
/// DTO zum Erstellen eines Sensor-Typs
/// </summary>
/// <param name="Code">Code (z.B. "temperature")</param>
/// <param name="Name">Anzeigename (z.B. "Temperatur")</param>
/// <param name="Unit">Einheit (z.B. "°C")</param>
/// <param name="Description">Beschreibung</param>
/// <param name="IconName">Material Icon Name</param>
public record CreateSensorTypeDto(
    string Code,
    string Name,
    string Unit,
    string? Description = null,
    string? IconName = null
);
