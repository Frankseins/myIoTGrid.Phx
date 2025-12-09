using FluentAssertions;
using myIoTGrid.Hub.Infrastructure.Matter;

namespace myIoTGrid.Hub.Service.Tests.Matter;

public class MatterDeviceMappingTests
{
    #region GetMatterDeviceType Tests

    [Theory]
    [InlineData("temperature", "temperature")]
    [InlineData("humidity", "humidity")]
    [InlineData("pressure", "pressure")]
    [InlineData("contact", "contact")]
    public void GetMatterDeviceType_WithSupportedType_ReturnsMatterType(string sensorType, string expectedMatterType)
    {
        // Act
        var result = MatterDeviceMapping.GetMatterDeviceType(sensorType);

        // Assert
        result.Should().Be(expectedMatterType);
    }

    [Theory]
    [InlineData("TEMPERATURE")]
    [InlineData("Temperature")]
    [InlineData("HUMIDITY")]
    [InlineData("Humidity")]
    public void GetMatterDeviceType_IsCaseInsensitive(string sensorType)
    {
        // Act
        var result = MatterDeviceMapping.GetMatterDeviceType(sensorType);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("co2")]
    [InlineData("pm25")]
    [InlineData("battery")]
    [InlineData("unknown")]
    [InlineData("")]
    public void GetMatterDeviceType_WithUnsupportedType_ReturnsNull(string sensorType)
    {
        // Act
        var result = MatterDeviceMapping.GetMatterDeviceType(sensorType);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsSupportedSensorType Tests

    [Theory]
    [InlineData("temperature")]
    [InlineData("humidity")]
    [InlineData("pressure")]
    public void IsSupportedSensorType_WithSupportedType_ReturnsTrue(string sensorType)
    {
        // Act
        var result = MatterDeviceMapping.IsSupportedSensorType(sensorType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("TEMPERATURE")]
    [InlineData("Temperature")]
    [InlineData("PRESSURE")]
    public void IsSupportedSensorType_IsCaseInsensitive(string sensorType)
    {
        // Act
        var result = MatterDeviceMapping.IsSupportedSensorType(sensorType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("contact")] // In dictionary but not in supported set
    [InlineData("co2")]
    [InlineData("pm25")]
    [InlineData("battery")]
    [InlineData("unknown")]
    [InlineData("")]
    public void IsSupportedSensorType_WithUnsupportedType_ReturnsFalse(string sensorType)
    {
        // Act
        var result = MatterDeviceMapping.IsSupportedSensorType(sensorType);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetSupportedSensorTypes Tests

    [Fact]
    public void GetSupportedSensorTypes_ReturnsExpectedTypes()
    {
        // Act
        var result = MatterDeviceMapping.GetSupportedSensorTypes();

        // Assert
        result.Should().Contain("temperature");
        result.Should().Contain("humidity");
        result.Should().Contain("pressure");
    }

    [Fact]
    public void GetSupportedSensorTypes_ReturnsThreeTypes()
    {
        // Act
        var result = MatterDeviceMapping.GetSupportedSensorTypes().ToList();

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region GenerateMatterDeviceId Tests

    [Fact]
    public void GenerateMatterDeviceId_ReturnsExpectedFormat()
    {
        // Arrange
        var sensorId = "sensor-123";
        var sensorTypeCode = "temperature";

        // Act
        var result = MatterDeviceMapping.GenerateMatterDeviceId(sensorId, sensorTypeCode);

        // Assert
        result.Should().Be("temperature-sensor-123");
    }

    [Fact]
    public void GenerateMatterDeviceId_WithDifferentTypes_ReturnsCorrectFormat()
    {
        // Arrange & Act
        var tempResult = MatterDeviceMapping.GenerateMatterDeviceId("s1", "temperature");
        var humidResult = MatterDeviceMapping.GenerateMatterDeviceId("s1", "humidity");

        // Assert
        tempResult.Should().Be("temperature-s1");
        humidResult.Should().Be("humidity-s1");
    }

    [Fact]
    public void GenerateMatterDeviceId_WithEmptyValues_StillGenerates()
    {
        // Act
        var result = MatterDeviceMapping.GenerateMatterDeviceId("", "");

        // Assert
        result.Should().Be("-");
    }

    #endregion

    #region GenerateAlertDeviceId Tests

    [Fact]
    public void GenerateAlertDeviceId_WithSensorId_ReturnsExpectedFormat()
    {
        // Arrange
        var alertTypeCode = "mold_risk";
        var sensorId = "sensor-123";

        // Act
        var result = MatterDeviceMapping.GenerateAlertDeviceId(alertTypeCode, sensorId);

        // Assert
        result.Should().Be("alert-mold_risk-sensor-123");
    }

    [Fact]
    public void GenerateAlertDeviceId_WithoutSensorId_ReturnsExpectedFormat()
    {
        // Arrange
        var alertTypeCode = "frost_warning";

        // Act
        var result = MatterDeviceMapping.GenerateAlertDeviceId(alertTypeCode);

        // Assert
        result.Should().Be("alert-frost_warning");
    }

    [Fact]
    public void GenerateAlertDeviceId_WithNullSensorId_ReturnsExpectedFormat()
    {
        // Arrange
        var alertTypeCode = "hub_offline";

        // Act
        var result = MatterDeviceMapping.GenerateAlertDeviceId(alertTypeCode, null);

        // Assert
        result.Should().Be("alert-hub_offline");
    }

    #endregion

    #region CreateDeviceDisplayName Tests

    [Fact]
    public void CreateDeviceDisplayName_WithLocation_ReturnsLocationBasedName()
    {
        // Arrange
        var name = "Sensor A";
        var location = "Wohnzimmer";
        var sensorType = "temperature";

        // Act
        var result = MatterDeviceMapping.CreateDeviceDisplayName(name, location, sensorType);

        // Assert
        result.Should().Be("Wohnzimmer: Temperatur");
    }

    [Fact]
    public void CreateDeviceDisplayName_WithoutLocation_ReturnsNameBasedName()
    {
        // Arrange
        var name = "Sensor A";
        var sensorType = "temperature";

        // Act
        var result = MatterDeviceMapping.CreateDeviceDisplayName(name, null, sensorType);

        // Assert
        result.Should().Be("Sensor A: Temperatur");
    }

    [Fact]
    public void CreateDeviceDisplayName_WithEmptyLocation_ReturnsNameBasedName()
    {
        // Arrange
        var name = "Sensor B";
        var sensorType = "humidity";

        // Act
        var result = MatterDeviceMapping.CreateDeviceDisplayName(name, "", sensorType);

        // Assert
        result.Should().Be("Sensor B: Luftfeuchte");
    }

    [Theory]
    [InlineData("temperature", "Temperatur")]
    [InlineData("humidity", "Luftfeuchte")]
    [InlineData("pressure", "Luftdruck")]
    public void CreateDeviceDisplayName_MapsTypeCorrectly(string sensorType, string expectedSuffix)
    {
        // Act
        var result = MatterDeviceMapping.CreateDeviceDisplayName("Sensor", null, sensorType);

        // Assert
        result.Should().EndWith(expectedSuffix);
    }

    [Fact]
    public void CreateDeviceDisplayName_WithUnknownType_UsesTypeAsIs()
    {
        // Arrange
        var sensorType = "co2";

        // Act
        var result = MatterDeviceMapping.CreateDeviceDisplayName("Sensor", null, sensorType);

        // Assert
        result.Should().Be("Sensor: co2");
    }

    #endregion

    #region CreateAlertDisplayName Tests

    [Fact]
    public void CreateAlertDisplayName_WithLocation_ReturnsLocationBasedName()
    {
        // Arrange
        var alertTypeName = "Schimmelrisiko";
        var location = "Bad";

        // Act
        var result = MatterDeviceMapping.CreateAlertDisplayName(alertTypeName, location);

        // Assert
        result.Should().Be("Bad: Schimmelrisiko");
    }

    [Fact]
    public void CreateAlertDisplayName_WithoutLocation_ReturnsAlertTypeName()
    {
        // Arrange
        var alertTypeName = "Frostwarnung";

        // Act
        var result = MatterDeviceMapping.CreateAlertDisplayName(alertTypeName, null);

        // Assert
        result.Should().Be("Frostwarnung");
    }

    [Fact]
    public void CreateAlertDisplayName_WithEmptyLocation_ReturnsAlertTypeName()
    {
        // Arrange
        var alertTypeName = "Hitzewarnung";

        // Act
        var result = MatterDeviceMapping.CreateAlertDisplayName(alertTypeName, "");

        // Assert
        result.Should().Be("Hitzewarnung");
    }

    #endregion
}

public class MatterRecordTypesTests
{
    #region MatterBridgeStatus Tests

    [Fact]
    public void MatterBridgeStatus_CanBeCreated()
    {
        // Arrange
        var devices = new List<MatterDeviceInfo>
        {
            new("sensor-1", "Sensor 1", "temperature", "Room 1"),
            new("sensor-2", "Sensor 2", "humidity", null)
        };

        // Act
        var status = new MatterBridgeStatus(
            IsStarted: true,
            DeviceCount: 2,
            Devices: devices,
            PairingCode: 12345678,
            Discriminator: 1234
        );

        // Assert
        status.IsStarted.Should().BeTrue();
        status.DeviceCount.Should().Be(2);
        status.Devices.Should().HaveCount(2);
        status.PairingCode.Should().Be(12345678);
        status.Discriminator.Should().Be(1234);
    }

    [Fact]
    public void MatterBridgeStatus_WithEmptyDevices_Works()
    {
        // Act
        var status = new MatterBridgeStatus(
            IsStarted: false,
            DeviceCount: 0,
            Devices: Array.Empty<MatterDeviceInfo>(),
            PairingCode: 0,
            Discriminator: 0
        );

        // Assert
        status.IsStarted.Should().BeFalse();
        status.DeviceCount.Should().Be(0);
        status.Devices.Should().BeEmpty();
    }

    [Fact]
    public void MatterBridgeStatus_Equality_Works()
    {
        // Arrange
        var devices = new List<MatterDeviceInfo>();
        var status1 = new MatterBridgeStatus(true, 0, devices, 123, 456);
        var status2 = new MatterBridgeStatus(true, 0, devices, 123, 456);

        // Assert
        status1.Should().Be(status2);
    }

    #endregion

    #region MatterDeviceInfo Tests

    [Fact]
    public void MatterDeviceInfo_CanBeCreated()
    {
        // Act
        var device = new MatterDeviceInfo(
            SensorId: "sensor-123",
            Name: "Temperature Sensor",
            Type: "temperature",
            Location: "Living Room"
        );

        // Assert
        device.SensorId.Should().Be("sensor-123");
        device.Name.Should().Be("Temperature Sensor");
        device.Type.Should().Be("temperature");
        device.Location.Should().Be("Living Room");
    }

    [Fact]
    public void MatterDeviceInfo_WithNullLocation_Works()
    {
        // Act
        var device = new MatterDeviceInfo(
            SensorId: "sensor-456",
            Name: "Humidity Sensor",
            Type: "humidity",
            Location: null
        );

        // Assert
        device.Location.Should().BeNull();
    }

    [Fact]
    public void MatterDeviceInfo_Equality_Works()
    {
        // Arrange
        var device1 = new MatterDeviceInfo("s1", "name", "type", "loc");
        var device2 = new MatterDeviceInfo("s1", "name", "type", "loc");

        // Assert
        device1.Should().Be(device2);
    }

    [Fact]
    public void MatterDeviceInfo_Inequality_Works()
    {
        // Arrange
        var device1 = new MatterDeviceInfo("s1", "name", "type", "loc1");
        var device2 = new MatterDeviceInfo("s1", "name", "type", "loc2");

        // Assert
        device1.Should().NotBe(device2);
    }

    #endregion

    #region MatterCommissionInfo Tests

    [Fact]
    public void MatterCommissionInfo_CanBeCreated()
    {
        // Act
        var info = new MatterCommissionInfo(
            PairingCode: 12345678,
            Discriminator: 1234,
            ManualPairingCode: "1234-567-8901",
            QrCodeData: "MT:Y.K9042C00KA0648G00"
        );

        // Assert
        info.PairingCode.Should().Be(12345678);
        info.Discriminator.Should().Be(1234);
        info.ManualPairingCode.Should().Be("1234-567-8901");
        info.QrCodeData.Should().Be("MT:Y.K9042C00KA0648G00");
    }

    [Fact]
    public void MatterCommissionInfo_Equality_Works()
    {
        // Arrange
        var info1 = new MatterCommissionInfo(123, 456, "code", "qr");
        var info2 = new MatterCommissionInfo(123, 456, "code", "qr");

        // Assert
        info1.Should().Be(info2);
    }

    #endregion

    #region MatterQrCodeInfo Tests

    [Fact]
    public void MatterQrCodeInfo_CanBeCreated()
    {
        // Act
        var info = new MatterQrCodeInfo(
            QrCodeData: "MT:Y.K9042C00KA0648G00",
            QrCodeImage: "base64encodedimage==",
            ManualPairingCode: "1234-567-8901"
        );

        // Assert
        info.QrCodeData.Should().Be("MT:Y.K9042C00KA0648G00");
        info.QrCodeImage.Should().Be("base64encodedimage==");
        info.ManualPairingCode.Should().Be("1234-567-8901");
    }

    [Fact]
    public void MatterQrCodeInfo_Equality_Works()
    {
        // Arrange
        var info1 = new MatterQrCodeInfo("qr", "img", "code");
        var info2 = new MatterQrCodeInfo("qr", "img", "code");

        // Assert
        info1.Should().Be(info2);
    }

    #endregion
}
