namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Warnstufe für Alerts (DTO)
/// </summary>
public enum AlertLevelDto
{
    /// <summary>Alles optimal (grün)</summary>
    Ok = 0,

    /// <summary>Hinweis/Tipp (blau)</summary>
    Info = 1,

    /// <summary>Warnung (gelb)</summary>
    Warning = 2,

    /// <summary>Kritisch (rot)</summary>
    Critical = 3
}
