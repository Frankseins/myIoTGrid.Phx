using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Shared.Constants;

/// <summary>
/// Predefined SensorTypes with Matter Cluster IDs for Seed-Data.
/// Standard Matter Clusters: 0x0400-0x04FF
/// Custom myIoTGrid Clusters: 0xFC00-0xFCFF
/// </summary>
public static class DefaultSensorTypes
{
    // ==================== Standard Matter Clusters ====================

    public static readonly CreateSensorTypeDto Temperature = new(
        TypeId: "temperature",
        DisplayName: "Temperatur",
        ClusterId: 0x0402,  // TemperatureMeasurement
        Unit: "°C",
        MatterClusterName: "TemperatureMeasurement",
        Resolution: 0.1,
        MinValue: -40,
        MaxValue: 125,
        Description: "Umgebungstemperatur",
        Category: "weather",
        Icon: "thermostat",
        Color: "#FF5722"
    );

    public static readonly CreateSensorTypeDto Humidity = new(
        TypeId: "humidity",
        DisplayName: "Luftfeuchtigkeit",
        ClusterId: 0x0405,  // RelativeHumidityMeasurement
        Unit: "%",
        MatterClusterName: "RelativeHumidityMeasurement",
        Resolution: 1,
        MinValue: 0,
        MaxValue: 100,
        Description: "Relative Luftfeuchtigkeit",
        Category: "weather",
        Icon: "water_drop",
        Color: "#2196F3"
    );

    public static readonly CreateSensorTypeDto Pressure = new(
        TypeId: "pressure",
        DisplayName: "Luftdruck",
        ClusterId: 0x0403,  // PressureMeasurement
        Unit: "hPa",
        MatterClusterName: "PressureMeasurement",
        Resolution: 0.1,
        MinValue: 300,
        MaxValue: 1100,
        Description: "Atmosphärischer Luftdruck",
        Category: "weather",
        Icon: "speed",
        Color: "#9C27B0"
    );

    public static readonly CreateSensorTypeDto Light = new(
        TypeId: "light",
        DisplayName: "Helligkeit",
        ClusterId: 0x0400,  // IlluminanceMeasurement
        Unit: "lux",
        MatterClusterName: "IlluminanceMeasurement",
        Resolution: 1,
        MinValue: 0,
        MaxValue: 100000,
        Description: "Lichtstärke/Helligkeit",
        Category: "weather",
        Icon: "light_mode",
        Color: "#FFC107"
    );

    public static readonly CreateSensorTypeDto Co2 = new(
        TypeId: "co2",
        DisplayName: "CO2",
        ClusterId: 0x040D,  // CarbonDioxideConcentrationMeasurement
        Unit: "ppm",
        MatterClusterName: "CarbonDioxideConcentrationMeasurement",
        Resolution: 1,
        MinValue: 400,
        MaxValue: 5000,
        Description: "Kohlendioxid-Konzentration",
        Category: "air",
        Icon: "air",
        Color: "#607D8B"
    );

    public static readonly CreateSensorTypeDto Pm25 = new(
        TypeId: "pm25",
        DisplayName: "Feinstaub PM2.5",
        ClusterId: 0x042A,  // Pm25ConcentrationMeasurement
        Unit: "µg/m³",
        MatterClusterName: "Pm25ConcentrationMeasurement",
        Resolution: 0.1,
        MinValue: 0,
        MaxValue: 500,
        Description: "Feinstaub mit Partikeldurchmesser unter 2.5 µm",
        Category: "air",
        Icon: "blur_on",
        Color: "#795548"
    );

    public static readonly CreateSensorTypeDto Pm10 = new(
        TypeId: "pm10",
        DisplayName: "Feinstaub PM10",
        ClusterId: 0x042D,  // Pm10ConcentrationMeasurement
        Unit: "µg/m³",
        MatterClusterName: "Pm10ConcentrationMeasurement",
        Resolution: 0.1,
        MinValue: 0,
        MaxValue: 500,
        Description: "Feinstaub mit Partikeldurchmesser unter 10 µm",
        Category: "air",
        Icon: "blur_on",
        Color: "#8D6E63"
    );

    // ==================== Custom myIoTGrid Clusters (0xFC00+) ====================

    public static readonly CreateSensorTypeDto WaterLevel = new(
        TypeId: "water_level",
        DisplayName: "Wasserstand",
        ClusterId: 0xFC00,  // Custom
        Unit: "cm",
        MatterClusterName: "myIoTGrid.WaterLevel",
        Resolution: 0.1,
        MinValue: 0,
        MaxValue: 1000,
        Description: "Wasserstand/Füllstand",
        IsCustom: true,
        Category: "water",
        Icon: "waves",
        Color: "#00BCD4"
    );

    public static readonly CreateSensorTypeDto WaterTemperature = new(
        TypeId: "water_temperature",
        DisplayName: "Wassertemperatur",
        ClusterId: 0xFC01,  // Custom
        Unit: "°C",
        MatterClusterName: "myIoTGrid.WaterTemperature",
        Resolution: 0.1,
        MinValue: -10,
        MaxValue: 50,
        Description: "Wassertemperatur",
        IsCustom: true,
        Category: "water",
        Icon: "thermostat",
        Color: "#00ACC1"
    );

    public static readonly CreateSensorTypeDto FlowVelocity = new(
        TypeId: "flow_velocity",
        DisplayName: "Fließgeschwindigkeit",
        ClusterId: 0xFC02,  // Custom
        Unit: "m/s",
        MatterClusterName: "myIoTGrid.FlowVelocity",
        Resolution: 0.01,
        MinValue: 0,
        MaxValue: 10,
        Description: "Fließgeschwindigkeit von Wasser",
        IsCustom: true,
        Category: "water",
        Icon: "water",
        Color: "#0097A7"
    );

    public static readonly CreateSensorTypeDto SoilMoisture = new(
        TypeId: "soil_moisture",
        DisplayName: "Bodenfeuchtigkeit",
        ClusterId: 0xFC10,  // Custom
        Unit: "%",
        MatterClusterName: "myIoTGrid.SoilMoisture",
        Resolution: 1,
        MinValue: 0,
        MaxValue: 100,
        Description: "Bodenfeuchtigkeit für Pflanzen",
        IsCustom: true,
        Category: "soil",
        Icon: "grass",
        Color: "#8BC34A"
    );

    public static readonly CreateSensorTypeDto SoilTemperature = new(
        TypeId: "soil_temperature",
        DisplayName: "Bodentemperatur",
        ClusterId: 0xFC11,  // Custom
        Unit: "°C",
        MatterClusterName: "myIoTGrid.SoilTemperature",
        Resolution: 0.1,
        MinValue: -20,
        MaxValue: 60,
        Description: "Temperatur des Bodens",
        IsCustom: true,
        Category: "soil",
        Icon: "thermostat",
        Color: "#689F38"
    );

    public static readonly CreateSensorTypeDto Uv = new(
        TypeId: "uv",
        DisplayName: "UV-Index",
        ClusterId: 0xFC20,  // Custom
        Unit: "index",
        MatterClusterName: "myIoTGrid.UVIndex",
        Resolution: 0.1,
        MinValue: 0,
        MaxValue: 15,
        Description: "UV-Strahlungsindex",
        IsCustom: true,
        Category: "weather",
        Icon: "wb_sunny",
        Color: "#FF9800"
    );

    public static readonly CreateSensorTypeDto WindSpeed = new(
        TypeId: "wind_speed",
        DisplayName: "Windgeschwindigkeit",
        ClusterId: 0xFC21,  // Custom
        Unit: "m/s",
        MatterClusterName: "myIoTGrid.WindSpeed",
        Resolution: 0.1,
        MinValue: 0,
        MaxValue: 50,
        Description: "Windgeschwindigkeit",
        IsCustom: true,
        Category: "weather",
        Icon: "air",
        Color: "#78909C"
    );

    public static readonly CreateSensorTypeDto WindDirection = new(
        TypeId: "wind_direction",
        DisplayName: "Windrichtung",
        ClusterId: 0xFC22,  // Custom
        Unit: "°",
        MatterClusterName: "myIoTGrid.WindDirection",
        Resolution: 1,
        MinValue: 0,
        MaxValue: 360,
        Description: "Windrichtung in Grad",
        IsCustom: true,
        Category: "weather",
        Icon: "explore",
        Color: "#546E7A"
    );

    public static readonly CreateSensorTypeDto Rainfall = new(
        TypeId: "rainfall",
        DisplayName: "Niederschlag",
        ClusterId: 0xFC23,  // Custom
        Unit: "mm",
        MatterClusterName: "myIoTGrid.Rainfall",
        Resolution: 0.1,
        MinValue: 0,
        MaxValue: 500,
        Description: "Niederschlagsmenge",
        IsCustom: true,
        Category: "weather",
        Icon: "water",
        Color: "#4FC3F7"
    );

    // ==================== System/Device Clusters ====================

    public static readonly CreateSensorTypeDto Battery = new(
        TypeId: "battery",
        DisplayName: "Batterie",
        ClusterId: 0x002F,  // PowerSource (Matter standard)
        Unit: "%",
        MatterClusterName: "PowerSource",
        Resolution: 1,
        MinValue: 0,
        MaxValue: 100,
        Description: "Batterie-Ladestand",
        Category: "system",
        Icon: "battery_full",
        Color: "#4CAF50"
    );

    public static readonly CreateSensorTypeDto Rssi = new(
        TypeId: "rssi",
        DisplayName: "Signalstärke",
        ClusterId: 0xFC30,  // Custom
        Unit: "dBm",
        MatterClusterName: "myIoTGrid.RSSI",
        Resolution: 1,
        MinValue: -120,
        MaxValue: 0,
        Description: "WLAN/LoRa Signalstärke",
        IsCustom: true,
        Category: "system",
        Icon: "signal_cellular_alt",
        Color: "#9E9E9E"
    );

    /// <summary>
    /// Returns all predefined SensorTypes
    /// </summary>
    public static IReadOnlyList<CreateSensorTypeDto> GetAll() =>
    [
        // Standard Matter
        Temperature,
        Humidity,
        Pressure,
        Light,
        Co2,
        Pm25,
        Pm10,
        // Water (Custom)
        WaterLevel,
        WaterTemperature,
        FlowVelocity,
        // Soil (Custom)
        SoilMoisture,
        SoilTemperature,
        // Weather (Custom)
        Uv,
        WindSpeed,
        WindDirection,
        Rainfall,
        // System
        Battery,
        Rssi
    ];

    /// <summary>
    /// Finds a SensorType by TypeId
    /// </summary>
    public static CreateSensorTypeDto? GetByTypeId(string typeId) =>
        GetAll().FirstOrDefault(s => s.TypeId.Equals(typeId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all SensorTypes for a specific category
    /// </summary>
    public static IEnumerable<CreateSensorTypeDto> GetByCategory(string category) =>
        GetAll().Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets all custom (non-Matter-standard) SensorTypes
    /// </summary>
    public static IEnumerable<CreateSensorTypeDto> GetCustomTypes() =>
        GetAll().Where(s => s.IsCustom);

    /// <summary>
    /// Gets all standard Matter SensorTypes
    /// </summary>
    public static IEnumerable<CreateSensorTypeDto> GetStandardTypes() =>
        GetAll().Where(s => !s.IsCustom);
}
