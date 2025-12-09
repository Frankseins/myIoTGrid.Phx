namespace myIoTGrid.Shared.Common.Constants;

/// <summary>
/// Standard sensor types and their units.
/// Used for validation and display purposes.
/// </summary>
public static class SensorTypes
{
    /// <summary>
    /// Mapping of sensor type codes to their standard units
    /// </summary>
    public static readonly Dictionary<string, string> Units = new()
    {
        ["temperature"] = "°C",
        ["humidity"] = "%",
        ["pressure"] = "hPa",
        ["water_level"] = "cm",
        ["flow_rate"] = "m/s",
        ["rainfall"] = "mm",
        ["co2"] = "ppm",
        ["pm25"] = "µg/m³",
        ["pm10"] = "µg/m³",
        ["pm1"] = "µg/m³",
        ["soil_moisture"] = "%",
        ["light"] = "lux",
        ["uv"] = "index",
        ["wind_speed"] = "m/s",
        ["wind_direction"] = "°",
        ["battery"] = "%",
        ["rssi"] = "dBm",
        ["voltage"] = "V",
        ["current"] = "A",
        ["power"] = "W",
        ["energy"] = "kWh",
        ["altitude"] = "m",
        ["speed"] = "km/h",
        ["distance"] = "cm",
        ["satellites"] = "",
        ["hdop"] = "",
        ["fix_type"] = ""
    };

    /// <summary>
    /// Gets the standard unit for a sensor type
    /// </summary>
    /// <param name="type">The sensor type code</param>
    /// <returns>The unit string or "unknown" if not found</returns>
    public static string GetUnit(string type)
        => Units.TryGetValue(type.ToLower(), out var unit) ? unit : "unknown";

    /// <summary>
    /// Validates if a sensor type is known
    /// </summary>
    /// <param name="type">The sensor type code to validate</param>
    /// <returns>True if this is a known sensor type</returns>
    public static bool IsValidType(string type)
        => Units.ContainsKey(type.ToLower());

    /// <summary>
    /// Gets all known sensor type codes
    /// </summary>
    public static IEnumerable<string> GetAllTypes()
        => Units.Keys;
}
