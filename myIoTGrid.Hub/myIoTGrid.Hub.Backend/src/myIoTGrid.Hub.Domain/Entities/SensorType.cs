using myIoTGrid.Hub.Domain.Interfaces;

namespace myIoTGrid.Hub.Domain.Entities;

/// <summary>
/// Sensor-Typ Definition (z.B. Temperatur, Luftfeuchtigkeit, CO2)
/// Wird von Grid.Cloud synchronisiert
/// </summary>
public class SensorType : ISyncableEntity
{
    /// <summary>Primärschlüssel</summary>
    public Guid Id { get; set; }

    /// <summary>Code für den Sensor-Typ (z.B. "temperature")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Anzeigename (z.B. "Temperatur")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Einheit (z.B. "°C")</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Beschreibung des Sensor-Typs</summary>
    public string? Description { get; set; }

    /// <summary>Material Icon Name für UI</summary>
    public string? IconName { get; set; }

    /// <summary>Ob dieser Typ global (von Cloud definiert) ist</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Erstellungszeitpunkt</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public ICollection<SensorData> SensorData { get; set; } = new List<SensorData>();
}
