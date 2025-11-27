namespace myIoTGrid.Hub.Infrastructure.Mqtt;

/// <summary>
/// Konfigurationsoptionen für den MQTT Client.
/// Wird aus appsettings.json geladen.
/// </summary>
public class MqttOptions
{
    /// <summary>
    /// Konfigurationsabschnittsname
    /// </summary>
    public const string SectionName = "Mqtt";

    /// <summary>
    /// Hostname oder IP-Adresse des MQTT Brokers
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Port des MQTT Brokers (Standard: 1883)
    /// </summary>
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Client-ID für die Verbindung zum Broker
    /// </summary>
    public string ClientId { get; set; } = "myiotgrid-hub-api";

    /// <summary>
    /// Benutzername für die Authentifizierung (optional)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Passwort für die Authentifizierung (optional)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Ob TLS verwendet werden soll
    /// </summary>
    public bool UseTls { get; set; } = false;

    /// <summary>
    /// Keep-Alive-Intervall in Sekunden
    /// </summary>
    public int KeepAliveSeconds { get; set; } = 30;

    /// <summary>
    /// Maximale Anzahl an Reconnect-Versuchen
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// Verzögerung zwischen Reconnect-Versuchen in Sekunden
    /// </summary>
    public int ReconnectDelaySeconds { get; set; } = 5;
}
