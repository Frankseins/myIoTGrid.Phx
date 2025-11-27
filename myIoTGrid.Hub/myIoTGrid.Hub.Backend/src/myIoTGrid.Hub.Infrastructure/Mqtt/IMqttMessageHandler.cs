namespace myIoTGrid.Hub.Infrastructure.Mqtt;

/// <summary>
/// Interface für die Verarbeitung eingehender MQTT-Nachrichten.
/// </summary>
public interface IMqttMessageHandler
{
    /// <summary>
    /// Verarbeitet eine eingehende MQTT-Nachricht.
    /// </summary>
    /// <param name="topic">Das Topic der Nachricht</param>
    /// <param name="payload">Der Payload als String (JSON)</param>
    /// <param name="ct">CancellationToken</param>
    /// <returns>True wenn die Nachricht erfolgreich verarbeitet wurde</returns>
    Task<bool> HandleMessageAsync(string topic, string payload, CancellationToken ct = default);

    /// <summary>
    /// Prüft, ob dieses Handler das angegebene Topic verarbeiten kann.
    /// </summary>
    /// <param name="topic">Das zu prüfende Topic</param>
    /// <returns>True wenn der Handler für dieses Topic zuständig ist</returns>
    bool CanHandle(string topic);
}
