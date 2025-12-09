using myIoTGrid.Shared.Common.Enums;
using myIoTGrid.Shared.Common.Interfaces;

namespace myIoTGrid.Shared.Common.Entities;

/// <summary>
/// Alert-Typ Definition (z.B. Schimmelrisiko, Frostwarnung)
/// Wird von Grid.Cloud synchronisiert
/// </summary>
public class AlertType : ISyncableEntity
{
    /// <summary>Primärschlüssel</summary>
    public Guid Id { get; set; }

    /// <summary>Code für den Alert-Typ (z.B. "mold_risk")</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Anzeigename (z.B. "Schimmelrisiko")</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beschreibung des Alert-Typs</summary>
    public string? Description { get; set; }

    /// <summary>Standard-Warnstufe für diesen Alert-Typ</summary>
    public AlertLevel DefaultLevel { get; set; }

    /// <summary>Material Icon Name für UI</summary>
    public string? IconName { get; set; }

    /// <summary>Ob dieser Typ global (von Cloud definiert) ist</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Erstellungszeitpunkt</summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
