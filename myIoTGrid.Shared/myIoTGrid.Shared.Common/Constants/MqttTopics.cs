namespace myIoTGrid.Shared.Common.Constants;

/// <summary>
/// MQTT Topic-Patterns für die Kommunikation
/// </summary>
public static class MqttTopics
{
    /// <summary>Basis-Prefix für alle myIoTGrid Topics</summary>
    public const string Prefix = "myiotgrid";

    /// <summary>
    /// Topic für neue Sensor-Daten
    /// Pattern: myiotgrid/{tenantId}/sensordata
    /// </summary>
    public const string SensorDataPattern = "{0}/{1}/sensordata";

    /// <summary>
    /// Topic für Hub-Status (online/offline)
    /// Pattern: myiotgrid/{tenantId}/hubs/{hubId}/status
    /// </summary>
    public const string HubStatusPattern = "{0}/{1}/hubs/{2}/status";

    /// <summary>
    /// Topic für Alerts
    /// Pattern: myiotgrid/{tenantId}/alerts
    /// </summary>
    public const string AlertsPattern = "{0}/{1}/alerts";

    /// <summary>
    /// Topic für ChirpStack LoRaWAN Events
    /// Pattern: application/+/device/+/event/up
    /// </summary>
    public const string ChirpStackUplink = "application/+/device/+/event/up";

    /// <summary>
    /// Wildcard für alle Sensor-Daten eines Tenants
    /// Pattern: myiotgrid/{tenantId}/sensordata
    /// </summary>
    public static string GetSensorDataWildcard(Guid tenantId)
        => $"{Prefix}/{tenantId}/sensordata";

    /// <summary>
    /// Wildcard für alle Hub-Status eines Tenants
    /// Pattern: myiotgrid/{tenantId}/hubs/+/status
    /// </summary>
    public static string GetHubStatusWildcard(Guid tenantId)
        => $"{Prefix}/{tenantId}/hubs/+/status";

    /// <summary>
    /// Konkretes Topic für Sensor-Daten
    /// </summary>
    public static string GetSensorDataTopic(Guid tenantId)
        => string.Format(SensorDataPattern, Prefix, tenantId);

    /// <summary>
    /// Konkretes Topic für Hub-Status
    /// </summary>
    public static string GetHubStatusTopic(Guid tenantId, string hubId)
        => string.Format(HubStatusPattern, Prefix, tenantId, hubId);

    /// <summary>
    /// Konkretes Topic für Alerts
    /// </summary>
    public static string GetAlertsTopic(Guid tenantId)
        => string.Format(AlertsPattern, Prefix, tenantId);
}
