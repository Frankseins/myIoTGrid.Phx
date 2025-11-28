using FluentAssertions;
using myIoTGrid.Hub.Shared.Constants;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;
using myIoTGrid.Hub.Shared.Options;

namespace myIoTGrid.Hub.Service.Tests.DTOs;

#region NodeDto Tests

/// <summary>
/// Tests for Node DTOs (ESP32/LoRa32 devices = Matter Nodes)
/// </summary>
public class NodeDtoTests
{
    [Fact]
    public void NodeDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new NodeDto(
            Id: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            NodeId: "node-01",
            Name: "Test Node",
            Protocol: ProtocolDto.WLAN,
            Location: new LocationDto("Living Room"),
            Sensors: new List<SensorDto>(),
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: "1.0.0",
            BatteryLevel: 85,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.Name.Should().Be("Test Node");
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.IsOnline.Should().BeTrue();
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.BatteryLevel.Should().Be(85);
        dto.Sensors.Should().BeEmpty();
    }

    [Fact]
    public void CreateNodeDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new CreateNodeDto(NodeId: "node-01");

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.Name.Should().BeNull();
        dto.HubIdentifier.Should().BeNull();
        dto.HubId.Should().BeNull();
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.Location.Should().BeNull();
    }

    [Fact]
    public void CreateNodeDto_ShouldAllowAllPropertiesToBeSet()
    {
        // Arrange & Act
        var hubId = Guid.NewGuid();
        var location = new LocationDto("Kitchen");
        var dto = new CreateNodeDto(
            NodeId: "node-kitchen-01",
            Name: "Kitchen Node",
            HubIdentifier: "hub-01",
            HubId: hubId,
            Protocol: ProtocolDto.LoRaWAN,
            Location: location
        );

        // Assert
        dto.NodeId.Should().Be("node-kitchen-01");
        dto.Name.Should().Be("Kitchen Node");
        dto.HubIdentifier.Should().Be("hub-01");
        dto.HubId.Should().Be(hubId);
        dto.Protocol.Should().Be(ProtocolDto.LoRaWAN);
        dto.Location.Should().Be(location);
    }

    [Fact]
    public void UpdateNodeDto_ShouldHaveDefaultNullValues()
    {
        // Arrange & Act
        var dto = new UpdateNodeDto();

        // Assert
        dto.Name.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.FirmwareVersion.Should().BeNull();
    }

    [Fact]
    public void NodeStatusDto_ShouldBeCreatedCorrectly()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var lastSeen = DateTime.UtcNow;

        // Act
        var dto = new NodeStatusDto(
            NodeId: nodeId,
            IsOnline: true,
            LastSeen: lastSeen,
            BatteryLevel: 50
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.IsOnline.Should().BeTrue();
        dto.LastSeen.Should().Be(lastSeen);
        dto.BatteryLevel.Should().Be(50);
    }

    [Fact]
    public void NodeStatusDto_ShouldAllowNullOptionalValues()
    {
        // Act
        var nodeId = Guid.NewGuid();
        var dto = new NodeStatusDto(
            NodeId: nodeId,
            IsOnline: false,
            LastSeen: null,
            BatteryLevel: null
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.IsOnline.Should().BeFalse();
        dto.LastSeen.Should().BeNull();
        dto.BatteryLevel.Should().BeNull();
    }
}

#endregion

#region SensorDto Tests (Physical Sensor Chip = Matter Endpoint)

/// <summary>
/// Tests for Sensor DTOs (physical sensor chips like DHT22, BME280 = Matter Endpoints)
/// </summary>
public class SensorDtoTests
{
    [Fact]
    public void SensorDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var sensorType = new SensorTypeDto(
            TypeId: "temperature",
            DisplayName: "Temperatur",
            ClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            Unit: "°C",
            Resolution: 0.1,
            MinValue: -40,
            MaxValue: 125,
            Description: "Temperature measurement",
            IsCustom: false,
            Category: "weather",
            Icon: "thermostat",
            Color: "#FF5722",
            IsGlobal: true,
            CreatedAt: DateTime.UtcNow
        );

        // Act
        var dto = new SensorDto(
            Id: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            SensorTypeId: "temperature",
            EndpointId: 1,
            Name: "Temperature Sensor",
            IsActive: true,
            SensorType: sensorType,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.SensorTypeId.Should().Be("temperature");
        dto.EndpointId.Should().Be(1);
        dto.Name.Should().Be("Temperature Sensor");
        dto.IsActive.Should().BeTrue();
        dto.SensorType.Should().NotBeNull();
        dto.SensorType!.Unit.Should().Be("°C");
    }

    [Fact]
    public void CreateSensorDto_ShouldHaveRequiredAndOptionalValues()
    {
        // Act
        var dto = new CreateSensorDto(
            SensorTypeId: "humidity",
            EndpointId: 2,
            Name: "Humidity Sensor"
        );

        // Assert
        dto.SensorTypeId.Should().Be("humidity");
        dto.EndpointId.Should().Be(2);
        dto.Name.Should().Be("Humidity Sensor");
    }

    [Fact]
    public void CreateSensorDto_ShouldAllowNullName()
    {
        // Act
        var dto = new CreateSensorDto(
            SensorTypeId: "co2",
            EndpointId: 3
        );

        // Assert
        dto.SensorTypeId.Should().Be("co2");
        dto.EndpointId.Should().Be(3);
        dto.Name.Should().BeNull();
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
    public void LocationDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new LocationDto(
            Name: "Garden",
            Latitude: 50.9375,
            Longitude: 6.9603
        );

        // Assert
        dto.Name.Should().Be("Garden");
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void LocationDto_ShouldAllowNullName()
    {
        // Act
        var dto = new LocationDto(
            Name: null,
            Latitude: 52.5200,
            Longitude: 13.4050
        );

        // Assert
        dto.Name.Should().BeNull();
        dto.Latitude.Should().Be(52.5200);
        dto.Longitude.Should().Be(13.4050);
    }

    [Fact]
    public void LocationDto_ShouldSupportRecordEquality()
    {
        // Arrange
        var dto1 = new LocationDto("Kitchen");
        var dto2 = new LocationDto("Kitchen");
        var dto3 = new LocationDto("Bathroom");

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
        // Arrange & Act
        var dto = new AlertDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            HubName: "Test Hub",
            NodeId: Guid.NewGuid(),
            NodeName: "Test Node",
            AlertTypeId: Guid.NewGuid(),
            AlertTypeCode: "mold_risk",
            AlertTypeName: "Mold Risk",
            Level: AlertLevelDto.Warning,
            Message: "Elevated mold risk detected",
            Recommendation: "Increase ventilation",
            Source: AlertSourceDto.Cloud,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddDays(1),
            AcknowledgedAt: null,
            IsActive: true
        );

        // Assert
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Message.Should().Be("Elevated mold risk detected");
        dto.Source.Should().Be(AlertSourceDto.Cloud);
        dto.IsActive.Should().BeTrue();
        dto.AcknowledgedAt.Should().BeNull();
    }

    [Fact]
    public void CreateAlertDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new CreateAlertDto(
            AlertTypeCode: "frost_warning",
            Message: "Frost warning detected"
        );

        // Assert
        dto.AlertTypeCode.Should().Be("frost_warning");
        dto.Message.Should().Be("Frost warning detected");
        dto.HubId.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Recommendation.Should().BeNull();
        dto.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void CreateAlertDto_ShouldAllowAllProperties()
    {
        // Act
        var expiresAt = DateTime.UtcNow.AddHours(6);
        var dto = new CreateAlertDto(
            AlertTypeCode: "battery_low",
            HubId: "hub-01",
            NodeId: "node-01",
            Level: AlertLevelDto.Critical,
            Message: "Battery critically low",
            Recommendation: "Replace battery immediately",
            ExpiresAt: expiresAt
        );

        // Assert
        dto.AlertTypeCode.Should().Be("battery_low");
        dto.HubId.Should().Be("hub-01");
        dto.NodeId.Should().Be("node-01");
        dto.Level.Should().Be(AlertLevelDto.Critical);
        dto.Recommendation.Should().Be("Replace battery immediately");
        dto.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void AlertFilterDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new AlertFilterDto();

        // Assert
        dto.HubId.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.AlertTypeCode.Should().BeNull();
        dto.Level.Should().BeNull();
        dto.Source.Should().BeNull();
        dto.IsActive.Should().BeNull();
        dto.IsAcknowledged.Should().BeNull();
        dto.From.Should().BeNull();
        dto.To.Should().BeNull();
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(50);
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

#region HubDto Tests

public class HubDtoTests
{
    [Fact]
    public void HubDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new HubDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: "hub-01",
            Name: "Main Hub",
            Description: "Main hub for the house",
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 5
        );

        // Assert
        dto.HubId.Should().Be("hub-01");
        dto.Name.Should().Be("Main Hub");
        dto.Description.Should().Be("Main hub for the house");
        dto.IsOnline.Should().BeTrue();
        dto.SensorCount.Should().Be(5);
    }

    [Fact]
    public void CreateHubDto_ShouldHaveRequiredAndOptionalValues()
    {
        // Act
        var dto = new CreateHubDto(HubId: "hub-new");

        // Assert
        dto.HubId.Should().Be("hub-new");
        dto.Name.Should().BeNull();
        dto.Description.Should().BeNull();
    }

    [Fact]
    public void UpdateHubDto_ShouldAllowPartialUpdates()
    {
        // Act
        var dto = new UpdateHubDto(
            Name: "Updated Hub Name",
            Description: null
        );

        // Assert
        dto.Name.Should().Be("Updated Hub Name");
        dto.Description.Should().BeNull();
    }
}

#endregion

#region ReadingDto Tests (Measurement = Matter Attribute Report)

public class ReadingDtoTests
{
    [Fact]
    public void ReadingDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new ReadingDto(
            Id: 1,
            TenantId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            SensorTypeId: "temperature",
            SensorTypeName: "Temperature",
            Value: 21.5,
            Unit: "°C",
            Timestamp: DateTime.UtcNow,
            Location: new LocationDto("Living Room"),
            IsSyncedToCloud: false
        );

        // Assert
        dto.SensorTypeId.Should().Be("temperature");
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public void CreateReadingDto_ShouldHaveRequiredAndOptionalValues()
    {
        // Act
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            Type: "temperature",
            Value: 22.3
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.Type.Should().Be("temperature");
        dto.Value.Should().Be(22.3);
        dto.HubId.Should().BeNull();
        dto.Timestamp.Should().BeNull();
    }

    [Fact]
    public void CreateReadingDto_WithAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            Type: "humidity",
            Value: 65.5,
            HubId: "hub-01",
            Timestamp: timestamp
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.Type.Should().Be("humidity");
        dto.Value.Should().Be(65.5);
        dto.HubId.Should().Be("hub-01");
        dto.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ReadingFilterDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new ReadingFilterDto();

        // Assert
        dto.NodeId.Should().BeNull();
        dto.NodeIdentifier.Should().BeNull();
        dto.HubId.Should().BeNull();
        dto.SensorTypeId.Should().BeNull();
        dto.From.Should().BeNull();
        dto.To.Should().BeNull();
        dto.IsSyncedToCloud.Should().BeNull();
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(50);
    }

    [Fact]
    public void ReadingFilterDto_ShouldAllowAllFilters()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act
        var dto = new ReadingFilterDto(
            NodeId: nodeId,
            NodeIdentifier: "node-01",
            HubId: hubId,
            SensorTypeId: "temperature",
            From: from,
            To: to,
            IsSyncedToCloud: false,
            Page: 2,
            PageSize: 100
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.NodeIdentifier.Should().Be("node-01");
        dto.HubId.Should().Be(hubId);
        dto.SensorTypeId.Should().Be("temperature");
        dto.From.Should().Be(from);
        dto.To.Should().Be(to);
        dto.IsSyncedToCloud.Should().BeFalse();
        dto.Page.Should().Be(2);
        dto.PageSize.Should().Be(100);
    }
}

#endregion

#region PaginatedResultDto Tests

public class PaginatedResultDtoTests
{
    [Fact]
    public void PaginatedResultDto_ShouldCalculatePagesCorrectly()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var dto = new PaginatedResultDto<string>(
            Items: items,
            TotalCount: 100,
            Page: 1,
            PageSize: 10
        );

        // Assert
        dto.Items.Should().HaveCount(3);
        dto.TotalCount.Should().Be(100);
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(10);
        dto.TotalPages.Should().Be(10);
        dto.HasPreviousPage.Should().BeFalse();
        dto.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResultDto_ShouldHandleMiddlePage()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var dto = new PaginatedResultDto<int>(
            Items: items,
            TotalCount: 50,
            Page: 3,
            PageSize: 10
        );

        // Assert
        dto.Page.Should().Be(3);
        dto.TotalPages.Should().Be(5);
        dto.HasPreviousPage.Should().BeTrue();
        dto.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResultDto_ShouldHandleLastPage()
    {
        // Arrange
        var items = new List<string> { "last" };

        // Act
        var dto = new PaginatedResultDto<string>(
            Items: items,
            TotalCount: 41,
            Page: 5,
            PageSize: 10
        );

        // Assert
        dto.TotalPages.Should().Be(5);
        dto.HasPreviousPage.Should().BeTrue();
        dto.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResultDto_ShouldHandleEmptyResult()
    {
        // Arrange
        var items = new List<string>();

        // Act
        var dto = new PaginatedResultDto<string>(
            Items: items,
            TotalCount: 0,
            Page: 1,
            PageSize: 10
        );

        // Assert
        dto.Items.Should().BeEmpty();
        dto.TotalCount.Should().Be(0);
        dto.TotalPages.Should().Be(0);
        dto.HasPreviousPage.Should().BeFalse();
        dto.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResultDto_ShouldHandleSinglePage()
    {
        // Arrange
        var items = new List<int> { 1, 2, 3 };

        // Act
        var dto = new PaginatedResultDto<int>(
            Items: items,
            TotalCount: 3,
            Page: 1,
            PageSize: 10
        );

        // Assert
        dto.TotalPages.Should().Be(1);
        dto.HasPreviousPage.Should().BeFalse();
        dto.HasNextPage.Should().BeFalse();
    }
}

#endregion

#region TenantDto Tests

public class TenantDtoTests
{
    [Fact]
    public void TenantDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new TenantDto(
            Id: Guid.NewGuid(),
            Name: "Test Tenant",
            CloudApiKey: "api-key-123",
            CreatedAt: DateTime.UtcNow,
            LastSyncAt: DateTime.UtcNow,
            IsActive: true
        );

        // Assert
        dto.Name.Should().Be("Test Tenant");
        dto.CloudApiKey.Should().Be("api-key-123");
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TenantDto_ShouldAllowNullOptionalValues()
    {
        // Act
        var dto = new TenantDto(
            Id: Guid.NewGuid(),
            Name: "Test Tenant",
            CloudApiKey: null,
            CreatedAt: DateTime.UtcNow,
            LastSyncAt: null,
            IsActive: true
        );

        // Assert
        dto.CloudApiKey.Should().BeNull();
        dto.LastSyncAt.Should().BeNull();
    }
}

#endregion

#region SensorTypeDto Tests

public class SensorTypeDtoTests
{
    [Fact]
    public void SensorTypeDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new SensorTypeDto(
            TypeId: "temperature",
            DisplayName: "Temperatur",
            ClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            Unit: "°C",
            Resolution: 0.1,
            MinValue: -40,
            MaxValue: 125,
            Description: "Temperature measurement",
            IsCustom: false,
            Category: "weather",
            Icon: "thermostat",
            Color: "#FF5722",
            IsGlobal: true,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.TypeId.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperatur");
        dto.ClusterId.Should().Be(0x0402u);
        dto.Unit.Should().Be("°C");
        dto.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void SensorTypeDto_ShouldAllowNullOptionalValues()
    {
        // Act
        var dto = new SensorTypeDto(
            TypeId: "custom_sensor",
            DisplayName: "Custom Sensor",
            ClusterId: 0xFC00,
            MatterClusterName: null,
            Unit: "units",
            Resolution: 1.0,
            MinValue: null,
            MaxValue: null,
            Description: null,
            IsCustom: true,
            Category: "other",
            Icon: null,
            Color: null,
            IsGlobal: false,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.TypeId.Should().Be("custom_sensor");
        dto.MatterClusterName.Should().BeNull();
        dto.IsCustom.Should().BeTrue();
        dto.IsGlobal.Should().BeFalse();
    }
}

#endregion

#region AlertTypeDto Tests

public class AlertTypeDtoTests
{
    [Fact]
    public void AlertTypeDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new AlertTypeDto(
            Id: Guid.NewGuid(),
            Code: "mold_risk",
            Name: "Mold Risk",
            Description: "Warning for elevated mold risk",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning",
            IsGlobal: true,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.Code.Should().Be("mold_risk");
        dto.Name.Should().Be("Mold Risk");
        dto.DefaultLevel.Should().Be(AlertLevelDto.Warning);
        dto.IsGlobal.Should().BeTrue();
    }
}

#endregion

#region DefaultSensorTypes Tests

public class DefaultSensorTypesTests
{
    [Fact]
    public void DefaultSensorTypes_ShouldContainExpectedTypes()
    {
        // Assert - verify key sensor types exist
        DefaultSensorTypes.GetAll().Should().Contain(st => st.TypeId == "temperature");
        DefaultSensorTypes.GetAll().Should().Contain(st => st.TypeId == "humidity");
        DefaultSensorTypes.GetAll().Should().Contain(st => st.TypeId == "pressure");
        DefaultSensorTypes.GetAll().Should().Contain(st => st.TypeId == "co2");
    }

    [Fact]
    public void DefaultSensorTypes_ShouldHaveValidMatterClusterIds()
    {
        // Temperature should have Matter Temperature Measurement cluster (0x0402)
        var temperature = DefaultSensorTypes.GetAll().First(st => st.TypeId == "temperature");
        temperature.ClusterId.Should().Be(0x0402u);

        // Humidity should have Matter Relative Humidity cluster (0x0405)
        var humidity = DefaultSensorTypes.GetAll().First(st => st.TypeId == "humidity");
        humidity.ClusterId.Should().Be(0x0405u);
    }

    [Fact]
    public void DefaultSensorTypes_AllShouldHaveUnits()
    {
        // Assert - all sensor types should have units defined
        foreach (var sensorType in DefaultSensorTypes.GetAll())
        {
            sensorType.Unit.Should().NotBeNullOrWhiteSpace($"{sensorType.TypeId} should have a unit");
        }
    }

    [Fact]
    public void DefaultSensorTypes_GetByTypeId_ShouldWork()
    {
        // Arrange & Act
        var temperature = DefaultSensorTypes.GetByTypeId("temperature");
        var nonExistent = DefaultSensorTypes.GetByTypeId("nonexistent");

        // Assert
        temperature.Should().NotBeNull();
        temperature!.TypeId.Should().Be("temperature");
        nonExistent.Should().BeNull();
    }
}

#endregion

#region DefaultAlertTypes Tests

public class DefaultAlertTypesTests
{
    [Fact]
    public void DefaultAlertTypes_ShouldContainExpectedTypes()
    {
        // Assert - verify key alert types exist
        DefaultAlertTypes.GetAll().Should().Contain(at => at.Code == "mold_risk");
        DefaultAlertTypes.GetAll().Should().Contain(at => at.Code == "frost_warning");
        DefaultAlertTypes.GetAll().Should().Contain(at => at.Code == "heat_warning");
        DefaultAlertTypes.GetAll().Should().Contain(at => at.Code == "battery_low");
        DefaultAlertTypes.GetAll().Should().Contain(at => at.Code == "sensor_offline");
        DefaultAlertTypes.GetAll().Should().Contain(at => at.Code == "hub_offline");
    }

    [Fact]
    public void DefaultAlertTypes_HubOffline_ShouldBeCritical()
    {
        // Arrange
        var hubOffline = DefaultAlertTypes.GetAll().First(at => at.Code == "hub_offline");

        // Assert
        hubOffline.DefaultLevel.Should().Be(AlertLevelDto.Critical);
    }

    [Fact]
    public void DefaultAlertTypes_AllShouldHaveValidCodes()
    {
        // Assert - all codes should be lowercase with underscores
        foreach (var alertType in DefaultAlertTypes.GetAll())
        {
            alertType.Code.Should().MatchRegex(@"^[a-z0-9_]+$",
                $"Alert type code '{alertType.Code}' should be lowercase with underscores");
        }
    }

    [Fact]
    public void DefaultAlertTypes_GetByCode_ShouldWork()
    {
        // Arrange & Act
        var moldRisk = DefaultAlertTypes.GetByCode("mold_risk");
        var nonExistent = DefaultAlertTypes.GetByCode("nonexistent");

        // Assert
        moldRisk.Should().NotBeNull();
        moldRisk!.Code.Should().Be("mold_risk");
        nonExistent.Should().BeNull();
    }
}

#endregion

#region MonitoringOptions Tests

public class MonitoringOptionsTests
{
    [Fact]
    public void MonitoringOptions_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new MonitoringOptions();

        // Assert
        options.NodeCheckIntervalSeconds.Should().Be(60);
        options.NodeOfflineTimeoutMinutes.Should().Be(5);
        options.HubCheckIntervalSeconds.Should().Be(60);
        options.HubOfflineTimeoutMinutes.Should().Be(5);
        options.DataRetentionIntervalHours.Should().Be(24);
        options.DataRetentionDays.Should().Be(30);
        options.EnableNodeMonitoring.Should().BeTrue();
        options.EnableHubMonitoring.Should().BeTrue();
        options.EnableDataRetention.Should().BeTrue();
    }

    [Fact]
    public void MonitoringOptions_ShouldAllowCustomValues()
    {
        // Arrange & Act
        var options = new MonitoringOptions
        {
            NodeCheckIntervalSeconds = 30,
            NodeOfflineTimeoutMinutes = 10,
            HubCheckIntervalSeconds = 120,
            HubOfflineTimeoutMinutes = 15,
            DataRetentionIntervalHours = 48,
            DataRetentionDays = 90,
            EnableNodeMonitoring = false,
            EnableHubMonitoring = false,
            EnableDataRetention = false
        };

        // Assert
        options.NodeCheckIntervalSeconds.Should().Be(30);
        options.NodeOfflineTimeoutMinutes.Should().Be(10);
        options.HubCheckIntervalSeconds.Should().Be(120);
        options.HubOfflineTimeoutMinutes.Should().Be(15);
        options.DataRetentionIntervalHours.Should().Be(48);
        options.DataRetentionDays.Should().Be(90);
        options.EnableNodeMonitoring.Should().BeFalse();
        options.EnableHubMonitoring.Should().BeFalse();
        options.EnableDataRetention.Should().BeFalse();
    }

    [Fact]
    public void MonitoringOptions_SectionName_ShouldBeCorrect()
    {
        // Assert
        MonitoringOptions.SectionName.Should().Be("Monitoring");
    }
}

#endregion

#region AlertLevel and Protocol Enum Tests

public class EnumDtoTests
{
    [Fact]
    public void AlertLevelDto_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<AlertLevelDto>().Should().HaveCount(4);
        AlertLevelDto.Ok.Should().Be((AlertLevelDto)0);
        AlertLevelDto.Info.Should().Be((AlertLevelDto)1);
        AlertLevelDto.Warning.Should().Be((AlertLevelDto)2);
        AlertLevelDto.Critical.Should().Be((AlertLevelDto)3);
    }

    [Fact]
    public void AlertSourceDto_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<AlertSourceDto>().Should().HaveCount(2);
        AlertSourceDto.Local.Should().Be((AlertSourceDto)0);
        AlertSourceDto.Cloud.Should().Be((AlertSourceDto)1);
    }

    [Fact]
    public void ProtocolDto_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<ProtocolDto>().Should().HaveCount(3);
        ProtocolDto.Unknown.Should().Be((ProtocolDto)0);
        ProtocolDto.WLAN.Should().Be((ProtocolDto)1);
        ProtocolDto.LoRaWAN.Should().Be((ProtocolDto)2);
    }
}

#endregion
