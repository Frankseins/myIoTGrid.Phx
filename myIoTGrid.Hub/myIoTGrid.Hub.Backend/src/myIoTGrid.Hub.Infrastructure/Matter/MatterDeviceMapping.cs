namespace myIoTGrid.Hub.Infrastructure.Matter;

/// <summary>
/// Maps sensor types to Matter device types
/// </summary>
public static class MatterDeviceMapping
{
    private static readonly Dictionary<string, string> SensorTypeToMatterType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["temperature"] = "temperature",
        ["humidity"] = "humidity",
        ["pressure"] = "pressure",
        ["contact"] = "contact",
    };

    private static readonly HashSet<string> SupportedSensorTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "temperature",
        "humidity",
        "pressure"
    };

    /// <summary>
    /// Get the Matter device type for a sensor type code
    /// </summary>
    public static string? GetMatterDeviceType(string sensorTypeCode)
    {
        return SensorTypeToMatterType.TryGetValue(sensorTypeCode, out var matterType)
            ? matterType
            : null;
    }

    /// <summary>
    /// Check if a sensor type is supported by Matter
    /// </summary>
    public static bool IsSupportedSensorType(string sensorTypeCode)
    {
        return SupportedSensorTypes.Contains(sensorTypeCode);
    }

    /// <summary>
    /// Get all supported sensor types
    /// </summary>
    public static IEnumerable<string> GetSupportedSensorTypes()
    {
        return SupportedSensorTypes;
    }

    /// <summary>
    /// Generate a unique Matter device ID for a sensor
    /// </summary>
    public static string GenerateMatterDeviceId(string sensorId, string sensorTypeCode)
    {
        return $"{sensorTypeCode}-{sensorId}";
    }

    /// <summary>
    /// Generate a unique Matter device ID for an alert
    /// </summary>
    public static string GenerateAlertDeviceId(string alertTypeCode, string? sensorId = null)
    {
        return sensorId != null
            ? $"alert-{alertTypeCode}-{sensorId}"
            : $"alert-{alertTypeCode}";
    }

    /// <summary>
    /// Create a display name for a Matter device
    /// </summary>
    public static string CreateDeviceDisplayName(string name, string? locationName, string sensorTypeCode)
    {
        var typeSuffix = sensorTypeCode switch
        {
            "temperature" => "Temperatur",
            "humidity" => "Luftfeuchte",
            "pressure" => "Luftdruck",
            _ => sensorTypeCode
        };

        if (!string.IsNullOrEmpty(locationName))
        {
            return $"{locationName}: {typeSuffix}";
        }

        return $"{name}: {typeSuffix}";
    }

    /// <summary>
    /// Create a display name for an alert Matter device
    /// </summary>
    public static string CreateAlertDisplayName(string alertTypeName, string? locationName)
    {
        if (!string.IsNullOrEmpty(locationName))
        {
            return $"{locationName}: {alertTypeName}";
        }

        return alertTypeName;
    }
}
