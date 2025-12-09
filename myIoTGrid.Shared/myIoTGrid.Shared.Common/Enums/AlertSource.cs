namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Quelle des Alerts (Domain)
/// </summary>
public enum AlertSource
{
    /// <summary>Lokale Regel (z.B. Hub offline)</summary>
    Local = 0,

    /// <summary>KI-Analyse aus Cloud</summary>
    Cloud = 1
}
