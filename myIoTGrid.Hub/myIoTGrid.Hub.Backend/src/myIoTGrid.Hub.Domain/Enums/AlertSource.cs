namespace myIoTGrid.Hub.Domain.Enums;

/// <summary>
/// Quelle des Alerts
/// </summary>
public enum AlertSource
{
    /// <summary>Lokale Regel (z.B. Hub offline)</summary>
    Local = 0,

    /// <summary>KI-Analyse aus Grid.Cloud</summary>
    Cloud = 1
}
