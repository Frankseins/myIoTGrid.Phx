namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Kommunikationsprotokoll des Sensors (Domain)
/// </summary>
public enum Protocol
{
    /// <summary>Unbekanntes Protokoll</summary>
    Unknown = 0,

    /// <summary>WLAN/WiFi (ESP32, ESP8266, Shelly)</summary>
    WLAN = 1,

    /// <summary>LoRaWAN (LoRa32, RAK, Dragino)</summary>
    LoRaWAN = 2
}
