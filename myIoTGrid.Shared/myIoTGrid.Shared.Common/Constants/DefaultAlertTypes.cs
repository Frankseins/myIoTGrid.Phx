using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Common.Enums;

namespace myIoTGrid.Shared.Common.Constants;

/// <summary>
/// Vordefinierte Alert-Typen für Seed-Daten
/// </summary>
public static class DefaultAlertTypes
{
    public static readonly CreateAlertTypeDto MoldRisk = new(
        Code: "mold_risk",
        Name: "Schimmelrisiko",
        Description: "Erhöhtes Schimmelrisiko durch hohe Luftfeuchtigkeit und niedrige Temperatur",
        DefaultLevel: AlertLevelDto.Warning,
        IconName: "warning"
    );

    public static readonly CreateAlertTypeDto FrostWarning = new(
        Code: "frost_warning",
        Name: "Frostwarnung",
        Description: "Temperatur unter Gefrierpunkt",
        DefaultLevel: AlertLevelDto.Critical,
        IconName: "ac_unit"
    );

    public static readonly CreateAlertTypeDto HeatWarning = new(
        Code: "heat_warning",
        Name: "Hitzewarnung",
        Description: "Temperatur über kritischem Schwellwert",
        DefaultLevel: AlertLevelDto.Warning,
        IconName: "local_fire_department"
    );

    public static readonly CreateAlertTypeDto AirQuality = new(
        Code: "air_quality",
        Name: "Luftqualität",
        Description: "Schlechte Luftqualität (CO2, Feinstaub)",
        DefaultLevel: AlertLevelDto.Info,
        IconName: "air"
    );

    public static readonly CreateAlertTypeDto BatteryLow = new(
        Code: "battery_low",
        Name: "Batterie niedrig",
        Description: "Batterie-Ladestand unter kritischem Schwellwert",
        DefaultLevel: AlertLevelDto.Warning,
        IconName: "battery_alert"
    );

    public static readonly CreateAlertTypeDto HubOffline = new(
        Code: "hub_offline",
        Name: "Hub offline",
        Description: "Raspberry Pi Gateway nicht erreichbar",
        DefaultLevel: AlertLevelDto.Critical,
        IconName: "signal_wifi_off"
    );

    public static readonly CreateAlertTypeDto SensorOffline = new(
        Code: "sensor_offline",
        Name: "Sensor offline",
        Description: "ESP32/LoRa32 Sensor nicht erreichbar",
        DefaultLevel: AlertLevelDto.Warning,
        IconName: "sensors_off"
    );

    public static readonly CreateAlertTypeDto SensorError = new(
        Code: "sensor_error",
        Name: "Sensor-Fehler",
        Description: "Sensor liefert ungültige oder fehlerhafte Daten",
        DefaultLevel: AlertLevelDto.Warning,
        IconName: "error"
    );

    public static readonly CreateAlertTypeDto ThresholdExceeded = new(
        Code: "threshold_exceeded",
        Name: "Schwellwert überschritten",
        Description: "Ein benutzerdefinierter Schwellwert wurde überschritten",
        DefaultLevel: AlertLevelDto.Info,
        IconName: "notifications"
    );

    /// <summary>
    /// Gibt alle vordefinierten Alert-Typen zurück
    /// </summary>
    public static IReadOnlyList<CreateAlertTypeDto> GetAll() =>
    [
        MoldRisk,
        FrostWarning,
        HeatWarning,
        AirQuality,
        BatteryLow,
        HubOffline,
        SensorOffline,
        SensorError,
        ThresholdExceeded
    ];

    /// <summary>
    /// Findet einen Alert-Typ anhand des Codes
    /// </summary>
    public static CreateAlertTypeDto? GetByCode(string code) =>
        GetAll().FirstOrDefault(a => a.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}
