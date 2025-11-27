using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Shared.Constants;

/// <summary>
/// Vordefinierte Sensor-Typen für Seed-Daten
/// </summary>
public static class DefaultSensorTypes
{
    public static readonly CreateSensorTypeDto Temperature = new(
        Code: "temperature",
        Name: "Temperatur",
        Unit: "°C",
        Description: "Umgebungstemperatur",
        IconName: "thermostat"
    );

    public static readonly CreateSensorTypeDto Humidity = new(
        Code: "humidity",
        Name: "Luftfeuchtigkeit",
        Unit: "%",
        Description: "Relative Luftfeuchtigkeit",
        IconName: "water_drop"
    );

    public static readonly CreateSensorTypeDto Pressure = new(
        Code: "pressure",
        Name: "Luftdruck",
        Unit: "hPa",
        Description: "Atmosphärischer Luftdruck",
        IconName: "speed"
    );

    public static readonly CreateSensorTypeDto Co2 = new(
        Code: "co2",
        Name: "CO2",
        Unit: "ppm",
        Description: "Kohlendioxid-Konzentration",
        IconName: "air"
    );

    public static readonly CreateSensorTypeDto Pm25 = new(
        Code: "pm25",
        Name: "Feinstaub PM2.5",
        Unit: "µg/m³",
        Description: "Feinstaub mit Partikeldurchmesser unter 2.5 µm",
        IconName: "blur_on"
    );

    public static readonly CreateSensorTypeDto Pm10 = new(
        Code: "pm10",
        Name: "Feinstaub PM10",
        Unit: "µg/m³",
        Description: "Feinstaub mit Partikeldurchmesser unter 10 µm",
        IconName: "blur_on"
    );

    public static readonly CreateSensorTypeDto SoilMoisture = new(
        Code: "soil_moisture",
        Name: "Bodenfeuchtigkeit",
        Unit: "%",
        Description: "Bodenfeuchtigkeit für Pflanzen",
        IconName: "grass"
    );

    public static readonly CreateSensorTypeDto Light = new(
        Code: "light",
        Name: "Helligkeit",
        Unit: "lux",
        Description: "Lichtstärke/Helligkeit",
        IconName: "light_mode"
    );

    public static readonly CreateSensorTypeDto Uv = new(
        Code: "uv",
        Name: "UV-Index",
        Unit: "index",
        Description: "UV-Strahlungsindex",
        IconName: "wb_sunny"
    );

    public static readonly CreateSensorTypeDto WindSpeed = new(
        Code: "wind_speed",
        Name: "Windgeschwindigkeit",
        Unit: "m/s",
        Description: "Windgeschwindigkeit",
        IconName: "air"
    );

    public static readonly CreateSensorTypeDto Rainfall = new(
        Code: "rainfall",
        Name: "Niederschlag",
        Unit: "mm",
        Description: "Niederschlagsmenge",
        IconName: "water"
    );

    public static readonly CreateSensorTypeDto WaterLevel = new(
        Code: "water_level",
        Name: "Wasserstand",
        Unit: "cm",
        Description: "Wasserstand/Füllstand",
        IconName: "waves"
    );

    public static readonly CreateSensorTypeDto Battery = new(
        Code: "battery",
        Name: "Batterie",
        Unit: "%",
        Description: "Batterie-Ladestand",
        IconName: "battery_full"
    );

    public static readonly CreateSensorTypeDto Rssi = new(
        Code: "rssi",
        Name: "Signalstärke",
        Unit: "dBm",
        Description: "WLAN/LoRa Signalstärke",
        IconName: "signal_cellular_alt"
    );

    /// <summary>
    /// Gibt alle vordefinierten Sensor-Typen zurück
    /// </summary>
    public static IReadOnlyList<CreateSensorTypeDto> GetAll() =>
    [
        Temperature,
        Humidity,
        Pressure,
        Co2,
        Pm25,
        Pm10,
        SoilMoisture,
        Light,
        Uv,
        WindSpeed,
        Rainfall,
        WaterLevel,
        Battery,
        Rssi
    ];

    /// <summary>
    /// Findet einen Sensor-Typ anhand des Codes
    /// </summary>
    public static CreateSensorTypeDto? GetByCode(string code) =>
        GetAll().FirstOrDefault(s => s.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
}
