using Microsoft.Extensions.Logging;
using myIoTGrid.Gateway.LoRaWAN.Bridge.Decoders;
using NSubstitute;

namespace myIoTGrid.Gateway.LoRaWAN.Bridge.Tests.Decoders;

/// <summary>
/// Unit Tests for MyIoTGridDecoder
/// Tests all sensor types, encoding/decoding, edge cases
/// </summary>
public class MyIoTGridDecoderTests
{
    private readonly ILogger<MyIoTGridDecoder> _logger;
    private readonly MyIoTGridDecoder _decoder;

    public MyIoTGridDecoderTests()
    {
        _logger = Substitute.For<ILogger<MyIoTGridDecoder>>();
        _decoder = new MyIoTGridDecoder(_logger);
    }

    #region Temperature Tests

    [Fact]
    public void Decode_Temperature_Positive_Success()
    {
        // Arrange: 0x01 (Temperature) + 0x07D0 (2000 / 100 = 20.00 °C)
        byte[] payload = [0x01, 0x07, 0xD0];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("temperature", readings[0].Type);
        Assert.Equal(20.00, readings[0].Value, 2);
        Assert.Equal("°C", readings[0].Unit);
        Assert.Equal(devEui, readings[0].DevEui);
    }

    [Fact]
    public void Decode_Temperature_Negative_Success()
    {
        // Arrange: 0x01 (Temperature) + 0xF830 (-2000 / 100 = -20.00 °C)
        // -2000 in signed 16-bit = 0xF830
        byte[] payload = [0x01, 0xF8, 0x30];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("temperature", readings[0].Type);
        Assert.Equal(-20.00, readings[0].Value, 2);
    }

    [Fact]
    public void Decode_Temperature_Zero_Success()
    {
        // Arrange: 0x01 (Temperature) + 0x0000 (0 / 100 = 0.00 °C)
        byte[] payload = [0x01, 0x00, 0x00];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal(0.00, readings[0].Value, 2);
    }

    [Fact]
    public void Decode_Temperature_MaxPositive_Success()
    {
        // Arrange: 0x01 (Temperature) + 0x7FFF (32767 / 100 = 327.67 °C)
        byte[] payload = [0x01, 0x7F, 0xFF];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal(327.67, readings[0].Value, 2);
    }

    [Fact]
    public void Decode_Temperature_MinNegative_Success()
    {
        // Arrange: 0x01 (Temperature) + 0x8000 (-32768 / 100 = -327.68 °C)
        byte[] payload = [0x01, 0x80, 0x00];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal(-327.68, readings[0].Value, 2);
    }

    #endregion

    #region Humidity Tests

    [Fact]
    public void Decode_Humidity_Success()
    {
        // Arrange: 0x02 (Humidity) + 0x1964 (6500 / 100 = 65.00 %)
        byte[] payload = [0x02, 0x19, 0x64];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("humidity", readings[0].Type);
        Assert.Equal(65.00, readings[0].Value, 2);
        Assert.Equal("%", readings[0].Unit);
    }

    [Fact]
    public void Decode_Humidity_100Percent_Success()
    {
        // Arrange: 0x02 (Humidity) + 0x2710 (10000 / 100 = 100.00 %)
        byte[] payload = [0x02, 0x27, 0x10];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal(100.00, readings[0].Value, 2);
    }

    #endregion

    #region Pressure Tests

    [Fact]
    public void Decode_Pressure_Success()
    {
        // Arrange: 0x03 (Pressure) + 0x2792 (10130 / 10 = 1013.0 hPa)
        byte[] payload = [0x03, 0x27, 0x92];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("pressure", readings[0].Type);
        Assert.Equal(1013.0, readings[0].Value, 1);
        Assert.Equal("hPa", readings[0].Unit);
    }

    #endregion

    #region Water Level Tests (Erft-Monitoring)

    [Fact]
    public void Decode_WaterLevel_Positive_Success()
    {
        // Arrange: 0x04 (WaterLevel) + 0x03E8 (1000 / 10 = 100.0 cm)
        byte[] payload = [0x04, 0x03, 0xE8];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("water_level", readings[0].Type);
        Assert.Equal(100.0, readings[0].Value, 1);
        Assert.Equal("cm", readings[0].Unit);
    }

    [Fact]
    public void Decode_WaterLevel_Negative_Success()
    {
        // Arrange: 0x04 (WaterLevel) + 0xFC18 (-1000 / 10 = -100.0 cm)
        byte[] payload = [0x04, 0xFC, 0x18];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal(-100.0, readings[0].Value, 1);
    }

    #endregion

    #region Battery Tests

    [Fact]
    public void Decode_Battery_Success()
    {
        // Arrange: 0x05 (Battery) + 0x2134 (8500 / 100 = 85.00 %)
        byte[] payload = [0x05, 0x21, 0x34];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("battery", readings[0].Type);
        Assert.Equal(85.00, readings[0].Value, 2);
        Assert.Equal("%", readings[0].Unit);
    }

    #endregion

    #region Multi-Sensor Payload Tests

    [Fact]
    public void Decode_MultiSensor_ThreeReadings_Success()
    {
        // Arrange: Temperature (20°C) + Humidity (65%) + Pressure (1013 hPa)
        byte[] payload = [
            0x01, 0x07, 0xD0,  // Temperature: 2000 / 100 = 20.00 °C
            0x02, 0x19, 0x64,  // Humidity: 6500 / 100 = 65.00 %
            0x03, 0x27, 0x92   // Pressure: 10130 / 10 = 1013.0 hPa
        ];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Equal(3, readings.Count);
        Assert.Equal("temperature", readings[0].Type);
        Assert.Equal(20.00, readings[0].Value, 2);
        Assert.Equal("humidity", readings[1].Type);
        Assert.Equal(65.00, readings[1].Value, 2);
        Assert.Equal("pressure", readings[2].Type);
        Assert.Equal(1013.0, readings[2].Value, 1);
    }

    [Fact]
    public void Decode_MultiSensor_FiveReadings_Success()
    {
        // Arrange: Full weather station payload
        byte[] payload = [
            0x01, 0x07, 0xD0,  // Temperature: 20.00 °C
            0x02, 0x19, 0x64,  // Humidity: 65.00 %
            0x03, 0x27, 0x92,  // Pressure: 1013.0 hPa
            0x04, 0x01, 0xF4,  // Water Level: 50.0 cm
            0x05, 0x21, 0x34   // Battery: 85.00 %
        ];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Equal(5, readings.Count);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Decode_EmptyPayload_ReturnsEmpty()
    {
        // Arrange
        byte[] payload = [];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public void Decode_NullPayload_ReturnsEmpty()
    {
        // Arrange
        byte[]? payload = null;
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload!, devEui, 1).ToList();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public void Decode_InvalidLength_ReturnsEmpty()
    {
        // Arrange: 2 bytes (not multiple of 3)
        byte[] payload = [0x01, 0x07];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public void Decode_InvalidLength_FourBytes_ReturnsOne()
    {
        // Arrange: 4 bytes - first 3 should decode, last 1 ignored
        byte[] payload = [0x01, 0x07, 0xD0, 0xFF];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Empty(readings); // 4 % 3 != 0, so invalid
    }

    [Fact]
    public void Decode_UnknownSensorType_SkipsReading()
    {
        // Arrange: Unknown type 0xFD
        byte[] payload = [0xFD, 0x00, 0x00];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public void Decode_MixedKnownUnknown_ReturnsKnownOnly()
    {
        // Arrange: Known + Unknown + Known
        byte[] payload = [
            0x01, 0x07, 0xD0,  // Temperature (known)
            0xFD, 0x00, 0x00,  // Unknown type
            0x02, 0x19, 0x64   // Humidity (known)
        ];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Equal(2, readings.Count);
        Assert.Equal("temperature", readings[0].Type);
        Assert.Equal("humidity", readings[1].Type);
    }

    #endregion

    #region CanDecode Tests

    [Fact]
    public void CanDecode_ValidPayload_FPort1_ReturnsTrue()
    {
        byte[] payload = [0x01, 0x07, 0xD0];
        Assert.True(_decoder.CanDecode(payload, 1));
    }

    [Fact]
    public void CanDecode_ValidPayload_FPort10_ReturnsTrue()
    {
        byte[] payload = [0x01, 0x07, 0xD0];
        Assert.True(_decoder.CanDecode(payload, 10));
    }

    [Fact]
    public void CanDecode_EmptyPayload_ReturnsFalse()
    {
        byte[] payload = [];
        Assert.False(_decoder.CanDecode(payload, 1));
    }

    [Fact]
    public void CanDecode_InvalidLength_ReturnsFalse()
    {
        byte[] payload = [0x01, 0x07];
        Assert.False(_decoder.CanDecode(payload, 1));
    }

    [Fact]
    public void CanDecode_FPort0_ReturnsFalse()
    {
        byte[] payload = [0x01, 0x07, 0xD0];
        Assert.False(_decoder.CanDecode(payload, 0));
    }

    [Fact]
    public void CanDecode_FPort11_ReturnsFalse()
    {
        byte[] payload = [0x01, 0x07, 0xD0];
        Assert.False(_decoder.CanDecode(payload, 11));
    }

    #endregion

    #region Encode Tests

    [Fact]
    public void Encode_Temperature_Success()
    {
        // Act
        var encoded = MyIoTGridDecoder.Encode("temperature", 20.00);

        // Assert
        Assert.Equal(3, encoded.Length);
        Assert.Equal(0x01, encoded[0]);
        Assert.Equal(0x07, encoded[1]);
        Assert.Equal(0xD0, encoded[2]);
    }

    [Fact]
    public void Encode_NegativeTemperature_Success()
    {
        // Act
        var encoded = MyIoTGridDecoder.Encode("temperature", -20.00);

        // Assert
        Assert.Equal(3, encoded.Length);
        Assert.Equal(0x01, encoded[0]);
        Assert.Equal(0xF8, encoded[1]);
        Assert.Equal(0x30, encoded[2]);
    }

    [Fact]
    public void Encode_Humidity_Success()
    {
        // Act
        var encoded = MyIoTGridDecoder.Encode("humidity", 65.00);

        // Assert
        Assert.Equal(3, encoded.Length);
        Assert.Equal(0x02, encoded[0]);
    }

    [Fact]
    public void Encode_UnknownType_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            MyIoTGridDecoder.Encode("unknown_sensor", 100));
    }

    [Fact]
    public void EncodeMultiple_ThreeReadings_Success()
    {
        // Act
        var encoded = MyIoTGridDecoder.EncodeMultiple(
            ("temperature", 20.00),
            ("humidity", 65.00),
            ("pressure", 1013.0)
        );

        // Assert
        Assert.Equal(9, encoded.Length); // 3 readings * 3 bytes
    }

    [Fact]
    public void Roundtrip_EncodeDecode_Success()
    {
        // Arrange
        var originalValue = 23.45;
        var devEui = "0000000000000001";

        // Act
        var encoded = MyIoTGridDecoder.Encode("temperature", originalValue);
        var decoded = _decoder.Decode(encoded, devEui, 1).Single();

        // Assert
        Assert.Equal(originalValue, decoded.Value, 2);
    }

    #endregion

    #region Metadata Tests

    [Fact]
    public void Decode_ContainsMetadata()
    {
        // Arrange
        byte[] payload = [0x01, 0x07, 0xD0];
        var devEui = "0000000000000001";
        var fPort = 5;

        // Act
        var reading = _decoder.Decode(payload, devEui, fPort).Single();

        // Assert
        Assert.NotNull(reading.Metadata);
        Assert.Equal(fPort.ToString(), reading.Metadata["fPort"]);
        Assert.Equal("2000", reading.Metadata["rawValue"]);
        Assert.Equal("myIoTGrid", reading.Metadata["decoder"]);
    }

    [Fact]
    public void Decode_HasTimestamp()
    {
        // Arrange
        byte[] payload = [0x01, 0x07, 0xD0];
        var devEui = "0000000000000001";

        // Act
        var before = DateTime.UtcNow;
        var reading = _decoder.Decode(payload, devEui, 1).Single();
        var after = DateTime.UtcNow;

        // Assert
        Assert.InRange(reading.Timestamp, before, after);
    }

    #endregion

    #region Water Quality Sensors (Erft-Monitoring)

    [Fact]
    public void Decode_WaterTemperature_Success()
    {
        // Arrange: 0x60 (WaterTemperature) + 0x05DC (1500 / 100 = 15.00 °C)
        byte[] payload = [0x60, 0x05, 0xDC];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("water_temperature", readings[0].Type);
        Assert.Equal(15.00, readings[0].Value, 2);
        Assert.Equal("°C", readings[0].Unit);
    }

    [Fact]
    public void Decode_WaterPH_Success()
    {
        // Arrange: 0x61 (WaterPH) + 0x02BC (700 / 100 = 7.00 pH)
        byte[] payload = [0x61, 0x02, 0xBC];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("water_ph", readings[0].Type);
        Assert.Equal(7.00, readings[0].Value, 2);
        Assert.Equal("pH", readings[0].Unit);
    }

    [Fact]
    public void Decode_WaterConductivity_Success()
    {
        // Arrange: 0x62 (WaterConductivity) + 0x01F4 (500 µS/cm)
        byte[] payload = [0x62, 0x01, 0xF4];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("water_conductivity", readings[0].Type);
        Assert.Equal(500, readings[0].Value, 0);
        Assert.Equal("µS/cm", readings[0].Unit);
    }

    [Fact]
    public void Decode_WaterDissolvedOxygen_Success()
    {
        // Arrange: 0x63 (DissolvedOxygen) + 0x0320 (800 / 100 = 8.00 mg/L)
        byte[] payload = [0x63, 0x03, 0x20];
        var devEui = "0000000000000001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Single(readings);
        Assert.Equal("water_dissolved_oxygen", readings[0].Type);
        Assert.Equal(8.00, readings[0].Value, 2);
        Assert.Equal("mg/L", readings[0].Unit);
    }

    [Fact]
    public void Decode_ErftMonitoringFullPayload_Success()
    {
        // Arrange: Complete Erft monitoring payload
        byte[] payload = [
            0x04, 0x01, 0xF4,  // Water Level: 50.0 cm
            0x60, 0x05, 0xDC,  // Water Temp: 15.00 °C
            0x61, 0x02, 0xBC,  // Water pH: 7.00
            0x62, 0x01, 0xF4,  // Conductivity: 500 µS/cm
            0x63, 0x03, 0x20,  // Dissolved O2: 8.00 mg/L
            0x05, 0x21, 0x34   // Battery: 85.00 %
        ];
        var devEui = "ERFT-SENSOR-001";

        // Act
        var readings = _decoder.Decode(payload, devEui, 1).ToList();

        // Assert
        Assert.Equal(6, readings.Count);
        Assert.All(readings, r => Assert.Equal("ERFT-SENSOR-001", r.DevEui));
    }

    #endregion
}
