namespace myIoTGrid.Shared.Common.Enums;

/// <summary>
/// Communication protocol used by sensors.
/// Determines pin configuration and data handling.
/// Values must match CommunicationProtocol (Domain) for direct casting.
/// </summary>
public enum CommunicationProtocolDto
{
    /// <summary>I²C Bus (SDA, SCL)</summary>
    I2C = 0,

    /// <summary>SPI Bus (MISO, MOSI, SCK, CS)</summary>
    SPI = 1,

    /// <summary>Dallas 1-Wire (single data pin)</summary>
    OneWire = 2,

    /// <summary>UART Serial (TX, RX)</summary>
    UART = 3,

    /// <summary>Analog input (ADC)</summary>
    Analog = 4,

    /// <summary>Digital GPIO (single pin)</summary>
    Digital = 5,

    /// <summary>Virtual/Simulated</summary>
    Virtual = 6,

    /// <summary>Ultrasonic (Trigger, Echo)</summary>
    UltraSonic = 7
}
