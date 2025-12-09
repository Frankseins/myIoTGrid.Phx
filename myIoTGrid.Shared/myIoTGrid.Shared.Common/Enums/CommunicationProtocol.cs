namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Communication protocol for sensors (Domain)
/// </summary>
public enum CommunicationProtocol
{
    /// <summary>I2C bus</summary>
    I2C = 0,

    /// <summary>SPI bus</summary>
    SPI = 1,

    /// <summary>OneWire bus</summary>
    OneWire = 2,

    /// <summary>UART serial</summary>
    UART = 3,

    /// <summary>Analog input</summary>
    Analog = 4,

    /// <summary>Digital GPIO</summary>
    Digital = 5,

    /// <summary>Virtual/Simulated</summary>
    Virtual = 6,

    /// <summary>Ultrasonic sensor (Trigger/Echo)</summary>
    UltraSonic = 7
}
