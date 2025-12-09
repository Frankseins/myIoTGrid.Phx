namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Quelle des Alerts (DTO)
/// </summary>
public enum AlertSourceDto
{
    /// <summary>Lokale Regel (z.B. Hub offline)</summary>
    Local = 0,

    /// <summary>KI-Analyse aus Grid.Cloud</summary>
    Cloud = 1
}
