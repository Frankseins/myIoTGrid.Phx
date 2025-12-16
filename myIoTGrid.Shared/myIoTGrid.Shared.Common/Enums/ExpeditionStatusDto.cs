namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Status einer Expedition (DTO)
/// </summary>
public enum ExpeditionStatusDto
{
    /// <summary>Geplant - Expedition noch nicht gestartet</summary>
    Planned = 0,

    /// <summary>Aktiv - Expedition läuft gerade</summary>
    Active = 1,

    /// <summary>Abgeschlossen - Expedition beendet</summary>
    Completed = 2,

    /// <summary>Archiviert - Expedition im Archiv</summary>
    Archived = 3
}
