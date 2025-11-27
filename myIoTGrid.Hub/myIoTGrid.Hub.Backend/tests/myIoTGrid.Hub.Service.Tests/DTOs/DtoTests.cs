using FluentAssertions;
using myIoTGrid.Hub.Shared.Constants;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Service.Tests.DTOs;

#region SensorDto Tests

public class SensorDtoTests
{
    [Fact]
    public void SensorDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new SensorDto(
            Id: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            SensorId: "sensor-01",
            Name: "Test Sensor",
            Protocol: ProtocolDto.WLAN,
            Location: new LocationDto("Living Room"),
            SensorTypes: ["temperature", "humidity"],
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: "1.0.0",
            BatteryLevel: 85,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.SensorId.Should().Be("sensor-01");
        dto.Name.Should().Be("Test Sensor");
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.IsOnline.Should().BeTrue();
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.BatteryLevel.Should().Be(85);
    }

    [Fact]
    public void CreateSensorDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new CreateSensorDto(SensorId: "sensor-01");

        // Assert
        dto.SensorId.Should().Be("sensor-01");
        dto.Name.Should().BeNull();
        dto.HubIdentifier.Should().BeNull();
        dto.HubId.Should().BeNull();
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.Location.Should().BeNull();
        dto.SensorTypes.Should().BeNull();
    }

    [Fact]
    public void CreateSensorDto_ShouldAllowAllPropertiesToBeSet()
    {
        // Arrange & Act
        var hubId = Guid.NewGuid();
        var location = new LocationDto("Kitchen");
        var dto = new CreateSensorDto(
            SensorId: "sensor-kitchen-01",
            Name: "Kitchen Sensor",
            HubIdentifier: "hub-01",
            HubId: hubId,
            Protocol: ProtocolDto.LoRaWAN,
            Location: location,
            SensorTypes: ["temperature"]
        );

        // Assert
        dto.SensorId.Should().Be("sensor-kitchen-01");
        dto.Name.Should().Be("Kitchen Sensor");
        dto.HubIdentifier.Should().Be("hub-01");
        dto.HubId.Should().Be(hubId);
        dto.Protocol.Should().Be(ProtocolDto.LoRaWAN);
        dto.Location.Should().Be(location);
        dto.SensorTypes.Should().ContainSingle().Which.Should().Be("temperature");
    }

    [Fact]
    public void UpdateSensorDto_ShouldHaveDefaultNullValues()
    {
        // Arrange & Act
        var dto = new UpdateSensorDto();

        // Assert
        dto.Name.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.SensorTypes.Should().BeNull();
        dto.FirmwareVersion.Should().BeNull();
    }

    [Fact]
    public void SensorStatusDto_ShouldBeCreatedCorrectly()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var lastSeen = DateTime.UtcNow;

        // Act
        var dto = new SensorStatusDto(
            SensorId: sensorId,
            IsOnline: true,
            LastSeen: lastSeen,
            BatteryLevel: 50
        );

        // Assert
        dto.SensorId.Should().Be(sensorId);
        dto.IsOnline.Should().BeTrue();
        dto.LastSeen.Should().Be(lastSeen);
        dto.BatteryLevel.Should().Be(50);
    }

    [Fact]
    public void SensorStatusDto_ShouldAllowNullOptionalValues()
    {
        // Arrange
        var sensorId = Guid.NewGuid();

        // Act
        var dto = new SensorStatusDto(
            SensorId: sensorId,
            IsOnline: false,
            LastSeen: null,
            BatteryLevel: null
        );

        // Assert
        dto.SensorId.Should().Be(sensorId);
        dto.IsOnline.Should().BeFalse();
        dto.LastSeen.Should().BeNull();
        dto.BatteryLevel.Should().BeNull();
    }
}

#endregion

#region LocationDto Tests

public class LocationDtoTests
{
    [Fact]
    public void LocationDto_ShouldBeCreatedWithNameOnly()
    {
        // Act
        var dto = new LocationDto(Name: "Living Room");

        // Assert
        dto.Name.Should().Be("Living Room");
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
    }

    [Fact]
    public void LocationDto_ShouldBeCreatedWithCoordinates()
    {
        // Act
        var dto = new LocationDto(Latitude: 50.9375, Longitude: 6.9603);

        // Assert
        dto.Name.Should().BeNull();
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void LocationDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new LocationDto(Name: "Cologne", Latitude: 50.9375, Longitude: 6.9603);

        // Assert
        dto.Name.Should().Be("Cologne");
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void LocationDto_ShouldSupportEquality()
    {
        // Arrange
        var dto1 = new LocationDto("Living Room", 50.9375, 6.9603);
        var dto2 = new LocationDto("Living Room", 50.9375, 6.9603);
        var dto3 = new LocationDto("Kitchen", 50.9375, 6.9603);

        // Assert
        dto1.Should().Be(dto2);
        dto1.Should().NotBe(dto3);
    }
}

#endregion

#region AlertDto Tests

public class AlertDtoTests
{
    [Fact]
    public void AlertDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var alertTypeId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var acknowledgedAt = DateTime.UtcNow;

        // Act
        var dto = new AlertDto(
            Id: id,
            TenantId: tenantId,
            HubId: hubId,
            HubName: "Test Hub",
            SensorId: sensorId,
            SensorName: "Test Sensor",
            AlertTypeId: alertTypeId,
            AlertTypeCode: "mold_risk",
            AlertTypeName: "Schimmelrisiko",
            Level: AlertLevelDto.Warning,
            Message: "High humidity detected",
            Recommendation: "Open windows",
            Source: AlertSourceDto.Cloud,
            CreatedAt: createdAt,
            ExpiresAt: expiresAt,
            AcknowledgedAt: acknowledgedAt,
            IsActive: true
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.AlertTypeId.Should().Be(alertTypeId);
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.AlertTypeName.Should().Be("Schimmelrisiko");
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Source.Should().Be(AlertSourceDto.Cloud);
        dto.Message.Should().Be("High humidity detected");
        dto.Recommendation.Should().Be("Open windows");
        dto.HubId.Should().Be(hubId);
        dto.HubName.Should().Be("Test Hub");
        dto.SensorId.Should().Be(sensorId);
        dto.SensorName.Should().Be("Test Sensor");
        dto.CreatedAt.Should().Be(createdAt);
        dto.ExpiresAt.Should().Be(expiresAt);
        dto.AcknowledgedAt.Should().Be(acknowledgedAt);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateAlertDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Test");

        // Assert
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.Message.Should().Be("Test");
        dto.Level.Should().Be(AlertLevelDto.Warning); // Default value
        dto.HubId.Should().BeNull();
        dto.SensorId.Should().BeNull();
        dto.Recommendation.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void CreateAlertDto_ShouldAllowAllPropertiesToBeSet()
    {
        // Act
        var expiresAt = DateTime.UtcNow.AddHours(2);
        var dto = new CreateAlertDto(
            AlertTypeCode: "frost_warning",
            Message: "Temperature below freezing",
            Level: AlertLevelDto.Critical,
            HubId: "hub-01",
            SensorId: "sensor-01",
            Recommendation: "Check heating",
            ExpiresAt: expiresAt
        );

        // Assert
        dto.AlertTypeCode.Should().Be("frost_warning");
        dto.Message.Should().Be("Temperature below freezing");
        dto.Level.Should().Be(AlertLevelDto.Critical);
        dto.HubId.Should().Be("hub-01");
        dto.SensorId.Should().Be("sensor-01");
        dto.Recommendation.Should().Be("Check heating");
        dto.ExpiresAt.Should().Be(expiresAt);
    }
}

#endregion

#region HubDto Tests

public class HubDtoTests
{
    [Fact]
    public void HubDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var lastSeen = DateTime.UtcNow;
        var createdAt = DateTime.UtcNow.AddDays(-1);

        // Act
        var dto = new HubDto(
            Id: id,
            TenantId: tenantId,
            HubId: "hub-01",
            Name: "Test Hub",
            Description: "A test hub",
            LastSeen: lastSeen,
            IsOnline: true,
            CreatedAt: createdAt,
            SensorCount: 5
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.HubId.Should().Be("hub-01");
        dto.Name.Should().Be("Test Hub");
        dto.Description.Should().Be("A test hub");
        dto.LastSeen.Should().Be(lastSeen);
        dto.IsOnline.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
        dto.SensorCount.Should().Be(5);
    }

    [Fact]
    public void CreateHubDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new CreateHubDto(HubId: "hub-01");

        // Assert
        dto.HubId.Should().Be("hub-01");
        dto.Name.Should().BeNull();
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateHubDto_ShouldHaveDefaultNullValues()
    {
        // Act
        var dto = new UpdateHubDto();

        // Assert
        dto.Name.Should().BeNull();
        dto.Description.Should().BeNull();
    }
}

#endregion

#region SensorDataDto Tests

public class SensorDataDtoTests
{
    [Fact]
    public void SensorDataDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var sensorTypeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new SensorDataDto(
            Id: id,
            TenantId: tenantId,
            SensorId: sensorId,
            SensorTypeId: sensorTypeId,
            SensorTypeCode: "temperature",
            SensorTypeName: "Temperatur",
            Unit: "°C",
            Value: 21.5,
            Timestamp: timestamp,
            Location: new LocationDto("Kitchen"),
            IsSyncedToCloud: false
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.TenantId.Should().Be(tenantId);
        dto.SensorId.Should().Be(sensorId);
        dto.SensorTypeId.Should().Be(sensorTypeId);
        dto.SensorTypeCode.Should().Be("temperature");
        dto.SensorTypeName.Should().Be("Temperatur");
        dto.Unit.Should().Be("°C");
        dto.Value.Should().Be(21.5);
        dto.Timestamp.Should().Be(timestamp);
        dto.Location.Should().NotBeNull();
        dto.IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public void CreateSensorDataDto_ShouldBeCreatedWithRequiredProperties()
    {
        // Act
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "temperature",
            Value: 22.5
        );

        // Assert
        dto.SensorId.Should().Be("sensor-01");
        dto.SensorType.Should().Be("temperature");
        dto.Value.Should().Be(22.5);
        dto.HubId.Should().BeNull();
        dto.Timestamp.Should().BeNull();
    }

    [Fact]
    public void CreateSensorDataDto_ShouldAllowHubIdAndTimestampToBeSet()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-01",
            SensorType: "humidity",
            Value: 65.0,
            HubId: "hub-01",
            Timestamp: timestamp
        );

        // Assert
        dto.HubId.Should().Be("hub-01");
        dto.Timestamp.Should().Be(timestamp);
    }
}

#endregion

#region PaginatedResultDto Tests

public class PaginatedResultDtoTests
{
    [Fact]
    public void PaginatedResultDto_ShouldCalculateTotalPagesCorrectly()
    {
        // Arrange & Act
        var dto = new PaginatedResultDto<string>(
            Items: ["a", "b", "c"],
            TotalCount: 25,
            Page: 1,
            PageSize: 10
        );

        // Assert
        dto.Items.Should().HaveCount(3);
        dto.TotalCount.Should().Be(25);
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.TotalPages.Should().Be(3); // 25/10 = 2.5 -> 3
    }

    [Fact]
    public void PaginatedResultDto_WithExactPages_ShouldCalculateCorrectly()
    {
        // Act
        var dto = new PaginatedResultDto<int>(
            Items: [1, 2, 3, 4, 5],
            TotalCount: 20,
            Page: 2,
            PageSize: 5
        );

        // Assert
        dto.TotalPages.Should().Be(4); // 20/5 = 4
    }

    [Fact]
    public void PaginatedResultDto_WithEmptyList_ShouldHaveZeroTotalPages()
    {
        // Act
        var dto = new PaginatedResultDto<string>(
            Items: [],
            TotalCount: 0,
            Page: 1,
            PageSize: 10
        );

        // Assert
        dto.TotalPages.Should().Be(0);
        dto.Items.Should().BeEmpty();
    }

    [Fact]
    public void PaginatedResultDto_ShouldSupportDifferentTypes()
    {
        // Act
        var dtoWithGuids = new PaginatedResultDto<Guid>(
            Items: [Guid.NewGuid(), Guid.NewGuid()],
            TotalCount: 2,
            Page: 1,
            PageSize: 10
        );

        // Assert
        dtoWithGuids.Items.Should().HaveCount(2);
    }
}

#endregion

#region AlertFilterDto Tests

public class AlertFilterDtoTests
{
    [Fact]
    public void AlertFilterDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new AlertFilterDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(50);
        dto.AlertTypeCode.Should().BeNull();
        dto.Level.Should().BeNull();
        dto.Source.Should().BeNull();
        dto.SensorId.Should().BeNull();
        dto.HubId.Should().BeNull();
        dto.IsActive.Should().BeNull();
        dto.IsAcknowledged.Should().BeNull();
        dto.From.Should().BeNull();
        dto.To.Should().BeNull();
    }

    [Fact]
    public void AlertFilterDto_ShouldAllowAllPropertiesToBeSet()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var dto = new AlertFilterDto(
            Page: 2,
            PageSize: 50,
            AlertTypeCode: "mold_risk",
            Level: AlertLevelDto.Warning,
            Source: AlertSourceDto.Cloud,
            SensorId: sensorId,
            HubId: hubId,
            IsActive: true,
            IsAcknowledged: false,
            From: from,
            To: to
        );

        // Assert
        dto.Page.Should().Be(2);
        dto.PageSize.Should().Be(50);
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Source.Should().Be(AlertSourceDto.Cloud);
        dto.SensorId.Should().Be(sensorId);
        dto.HubId.Should().Be(hubId);
        dto.IsActive.Should().BeTrue();
        dto.IsAcknowledged.Should().BeFalse();
        dto.From.Should().Be(from);
        dto.To.Should().Be(to);
    }
}

#endregion

#region SensorDataFilterDto Tests

public class SensorDataFilterDtoTests
{
    [Fact]
    public void SensorDataFilterDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new SensorDataFilterDto();

        // Assert
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(50);
        dto.SensorId.Should().BeNull();
        dto.SensorIdentifier.Should().BeNull();
        dto.SensorTypeCode.Should().BeNull();
        dto.From.Should().BeNull();
        dto.To.Should().BeNull();
        dto.IsSyncedToCloud.Should().BeNull();
    }

    [Fact]
    public void SensorDataFilterDto_ShouldAllowAllPropertiesToBeSet()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-1);
        var to = DateTime.UtcNow;

        // Act
        var dto = new SensorDataFilterDto(
            Page: 3,
            PageSize: 500,
            SensorId: sensorId,
            SensorIdentifier: "sensor-01",
            HubId: hubId,
            SensorTypeCode: "temperature",
            From: from,
            To: to,
            IsSyncedToCloud: false
        );

        // Assert
        dto.Page.Should().Be(3);
        dto.PageSize.Should().Be(500);
        dto.SensorId.Should().Be(sensorId);
        dto.SensorIdentifier.Should().Be("sensor-01");
        dto.HubId.Should().Be(hubId);
        dto.SensorTypeCode.Should().Be("temperature");
        dto.From.Should().Be(from);
        dto.To.Should().Be(to);
        dto.IsSyncedToCloud.Should().BeFalse();
    }
}

#endregion

#region TenantDto Tests

public class TenantDtoTests
{
    [Fact]
    public void TenantDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var lastSyncAt = DateTime.UtcNow;

        // Act
        var dto = new TenantDto(
            Id: id,
            Name: "Test Tenant",
            CloudApiKey: "****key",
            CreatedAt: createdAt,
            LastSyncAt: lastSyncAt,
            IsActive: true
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.Name.Should().Be("Test Tenant");
        dto.CloudApiKey.Should().Be("****key");
        dto.IsActive.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
        dto.LastSyncAt.Should().Be(lastSyncAt);
    }

    [Fact]
    public void CreateTenantDto_ShouldBeCreatedCorrectly()
    {
        // Act
        var dto = new CreateTenantDto(Name: "New Tenant", CloudApiKey: "api-key-123");

        // Assert
        dto.Name.Should().Be("New Tenant");
        dto.CloudApiKey.Should().Be("api-key-123");
    }

    [Fact]
    public void CreateTenantDto_ShouldHaveDefaultNullCloudApiKey()
    {
        // Act
        var dto = new CreateTenantDto(Name: "Tenant");

        // Assert
        dto.Name.Should().Be("Tenant");
        dto.CloudApiKey.Should().BeNull();
    }

    [Fact]
    public void UpdateTenantDto_ShouldHaveDefaultNullValues()
    {
        // Act
        var dto = new UpdateTenantDto();

        // Assert
        dto.Name.Should().BeNull();
        dto.CloudApiKey.Should().BeNull();
        dto.IsActive.Should().BeNull();
    }
}

#endregion

#region AlertTypeDto Tests

public class AlertTypeDtoTests
{
    [Fact]
    public void AlertTypeDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new AlertTypeDto(
            Id: id,
            Code: "mold_risk",
            Name: "Schimmelrisiko",
            Description: "Schimmelrisiko durch hohe Luftfeuchtigkeit",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning",
            IsGlobal: true,
            CreatedAt: createdAt
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.Code.Should().Be("mold_risk");
        dto.Name.Should().Be("Schimmelrisiko");
        dto.Description.Should().Be("Schimmelrisiko durch hohe Luftfeuchtigkeit");
        dto.DefaultLevel.Should().Be(AlertLevelDto.Warning);
        dto.IconName.Should().Be("warning");
        dto.IsGlobal.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void CreateAlertTypeDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new CreateAlertTypeDto(Code: "custom_alert", Name: "Custom Alert");

        // Assert
        dto.Code.Should().Be("custom_alert");
        dto.Name.Should().Be("Custom Alert");
        dto.Description.Should().BeNull();
        dto.DefaultLevel.Should().Be(AlertLevelDto.Warning); // Default is Warning
        dto.IconName.Should().BeNull();
    }
}

#endregion

#region SensorTypeDto Tests

public class SensorTypeDtoTests
{
    [Fact]
    public void SensorTypeDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var dto = new SensorTypeDto(
            Id: id,
            Code: "temperature",
            Name: "Temperatur",
            Unit: "°C",
            Description: "Temperaturmessung",
            IconName: "thermostat",
            IsGlobal: true,
            CreatedAt: createdAt
        );

        // Assert
        dto.Id.Should().Be(id);
        dto.Code.Should().Be("temperature");
        dto.Name.Should().Be("Temperatur");
        dto.Unit.Should().Be("°C");
        dto.Description.Should().Be("Temperaturmessung");
        dto.IconName.Should().Be("thermostat");
        dto.IsGlobal.Should().BeTrue();
        dto.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void CreateSensorTypeDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new CreateSensorTypeDto(Code: "custom", Name: "Custom", Unit: "units");

        // Assert
        dto.Code.Should().Be("custom");
        dto.Name.Should().Be("Custom");
        dto.Unit.Should().Be("units");
        dto.Description.Should().BeNull();
        dto.IconName.Should().BeNull();
    }
}

#endregion

#region DefaultSensorTypes Tests

public class DefaultSensorTypesTests
{
    [Fact]
    public void GetAll_ShouldReturnAllDefaultSensorTypes()
    {
        // Act
        var types = DefaultSensorTypes.GetAll();

        // Assert
        types.Should().NotBeEmpty();
        types.Count.Should().BeGreaterThan(10);
    }

    [Theory]
    [InlineData("temperature", "Temperatur", "°C")]
    [InlineData("humidity", "Luftfeuchtigkeit", "%")]
    [InlineData("pressure", "Luftdruck", "hPa")]
    [InlineData("co2", "CO2", "ppm")]
    [InlineData("pm25", "Feinstaub PM2.5", "µg/m³")]
    public void GetAll_ShouldContainExpectedTypes(string code, string name, string unit)
    {
        // Act
        var types = DefaultSensorTypes.GetAll();
        var type = types.FirstOrDefault(t => t.Code == code);

        // Assert
        type.Should().NotBeNull();
        type!.Name.Should().Be(name);
        type.Unit.Should().Be(unit);
    }

    [Theory]
    [InlineData("temperature")]
    [InlineData("humidity")]
    [InlineData("TEMPERATURE")]
    [InlineData("Humidity")]
    public void GetByCode_ShouldFindTypeCaseInsensitive(string code)
    {
        // Act
        var type = DefaultSensorTypes.GetByCode(code);

        // Assert
        type.Should().NotBeNull();
    }

    [Fact]
    public void GetByCode_WithNonExistentCode_ShouldReturnNull()
    {
        // Act
        var type = DefaultSensorTypes.GetByCode("nonexistent");

        // Assert
        type.Should().BeNull();
    }

    [Fact]
    public void Temperature_ShouldHaveCorrectValues()
    {
        // Assert
        DefaultSensorTypes.Temperature.Code.Should().Be("temperature");
        DefaultSensorTypes.Temperature.Name.Should().Be("Temperatur");
        DefaultSensorTypes.Temperature.Unit.Should().Be("°C");
    }

    [Fact]
    public void Humidity_ShouldHaveCorrectValues()
    {
        // Assert
        DefaultSensorTypes.Humidity.Code.Should().Be("humidity");
        DefaultSensorTypes.Humidity.Name.Should().Be("Luftfeuchtigkeit");
        DefaultSensorTypes.Humidity.Unit.Should().Be("%");
    }
}

#endregion

#region DefaultAlertTypes Tests

public class DefaultAlertTypesTests
{
    [Fact]
    public void GetAll_ShouldReturnAllDefaultAlertTypes()
    {
        // Act
        var types = DefaultAlertTypes.GetAll();

        // Assert
        types.Should().NotBeEmpty();
        types.Count.Should().BeGreaterOrEqualTo(8);
    }

    [Theory]
    [InlineData("mold_risk", "Schimmelrisiko")]
    [InlineData("frost_warning", "Frostwarnung")]
    [InlineData("hub_offline", "Hub offline")]
    [InlineData("battery_low", "Batterie niedrig")]
    public void GetAll_ShouldContainExpectedTypes(string code, string name)
    {
        // Act
        var types = DefaultAlertTypes.GetAll();
        var type = types.FirstOrDefault(t => t.Code == code);

        // Assert
        type.Should().NotBeNull();
        type!.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("mold_risk")]
    [InlineData("MOLD_RISK")]
    [InlineData("Mold_Risk")]
    public void GetByCode_ShouldFindTypeCaseInsensitive(string code)
    {
        // Act
        var type = DefaultAlertTypes.GetByCode(code);

        // Assert
        type.Should().NotBeNull();
    }

    [Fact]
    public void GetByCode_WithNonExistentCode_ShouldReturnNull()
    {
        // Act
        var type = DefaultAlertTypes.GetByCode("nonexistent");

        // Assert
        type.Should().BeNull();
    }

    [Fact]
    public void MoldRisk_ShouldHaveCorrectValues()
    {
        // Assert
        DefaultAlertTypes.MoldRisk.Code.Should().Be("mold_risk");
        DefaultAlertTypes.MoldRisk.Name.Should().Be("Schimmelrisiko");
        DefaultAlertTypes.MoldRisk.DefaultLevel.Should().Be(AlertLevelDto.Warning);
    }

    [Fact]
    public void FrostWarning_ShouldHaveCorrectValues()
    {
        // Assert
        DefaultAlertTypes.FrostWarning.Code.Should().Be("frost_warning");
        DefaultAlertTypes.FrostWarning.Name.Should().Be("Frostwarnung");
        DefaultAlertTypes.FrostWarning.DefaultLevel.Should().Be(AlertLevelDto.Critical);
    }

    [Fact]
    public void HubOffline_ShouldHaveCorrectValues()
    {
        // Assert
        DefaultAlertTypes.HubOffline.Code.Should().Be("hub_offline");
        DefaultAlertTypes.HubOffline.Name.Should().Be("Hub offline");
        DefaultAlertTypes.HubOffline.DefaultLevel.Should().Be(AlertLevelDto.Critical);
    }

    [Fact]
    public void SensorOffline_ShouldHaveCorrectValues()
    {
        // Assert
        DefaultAlertTypes.SensorOffline.Code.Should().Be("sensor_offline");
        DefaultAlertTypes.SensorOffline.Name.Should().Be("Sensor offline");
        DefaultAlertTypes.SensorOffline.DefaultLevel.Should().Be(AlertLevelDto.Warning);
    }

    [Fact]
    public void AcknowledgeAlertDto_ShouldBeCreatedCorrectly()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        // Act
        var dto = new AcknowledgeAlertDto(AlertId: alertId);

        // Assert
        dto.AlertId.Should().Be(alertId);
    }
}

#endregion
