using FluentAssertions;

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
        // New model: NodeDto has AssignmentCount instead of Sensors collection
        var dto = new NodeDto(
            Id: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            NodeId: "node-01",
            Name: "Test Node",
            Protocol: ProtocolDto.WLAN,
            Location: new LocationDto("Living Room"),
            AssignmentCount: 3,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: "1.0.0",
            BatteryLevel: 85,
            CreatedAt: DateTime.UtcNow,
            MacAddress: "AA:BB:CC:DD:EE:FF",
            Status: NodeProvisioningStatusDto.Configured,
            IsSimulation: false,
            StorageMode: StorageModeDto.RemoteOnly,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null,
            DebugLevel: DebugLevelDto.Normal,
            EnableRemoteLogging: false,
            LastDebugChange: null
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.Name.Should().Be("Test Node");
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.IsOnline.Should().BeTrue();
        dto.FirmwareVersion.Should().Be("1.0.0");
        dto.BatteryLevel.Should().Be(85);
        dto.AssignmentCount.Should().Be(3);
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

#region SensorDto Tests (v3.0 Two-Tier Model)

/// <summary>
/// Tests for Sensor DTOs (complete sensor definitions with hardware config and calibration)
/// v3.0 Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public class SensorDtoTests
{
    [Fact]
    public void SensorDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act - v3.0: SensorDto is complete sensor definition
        var capabilities = new List<SensorCapabilityDto>
        {
            new SensorCapabilityDto(
                Id: Guid.NewGuid(),
                SensorId: Guid.NewGuid(),
                MeasurementType: "temperature",
                DisplayName: "Temperature",
                Unit: "°C",
                MinValue: -40,
                MaxValue: 80,
                Resolution: 0.1,
                Accuracy: 0.5,
                MatterClusterId: 0x0402,
                MatterClusterName: "TemperatureMeasurement",
                SortOrder: 0,
                IsActive: true
            )
        };

        var dto = new SensorDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            Code: "dht22-wohnzimmer",
            Name: "Living Room DHT22",
            Description: "Temperature and humidity sensor in living room",
            SerialNumber: "DHT22-001",
            Manufacturer: "Aosong",
            Model: "DHT22",
            DatasheetUrl: "https://example.com/dht22.pdf",
            Protocol: CommunicationProtocolDto.OneWire,
            I2CAddress: null,
            SdaPin: null,
            SclPin: null,
            OneWirePin: 4,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            BaudRate: null,
            IntervalSeconds: 60,
            MinIntervalSeconds: 2,
            WarmupTimeMs: 1000,
            OffsetCorrection: 0.5,
            GainCorrection: 1.02,
            LastCalibratedAt: DateTime.UtcNow.AddMonths(-1),
            CalibrationNotes: "Calibrated with reference thermometer",
            CalibrationDueAt: DateTime.UtcNow.AddMonths(5),
            Category: "climate",
            Icon: "thermostat",
            Color: "#FF5722",
            Capabilities: capabilities,
            IsActive: true,
            CreatedAt: DateTime.UtcNow.AddDays(-10),
            UpdatedAt: DateTime.UtcNow
        );

        // Assert
        dto.Code.Should().Be("dht22-wohnzimmer");
        dto.Name.Should().Be("Living Room DHT22");
        dto.Description.Should().Be("Temperature and humidity sensor in living room");
        dto.SerialNumber.Should().Be("DHT22-001");
        dto.Protocol.Should().Be(CommunicationProtocolDto.OneWire);
        dto.IntervalSeconds.Should().Be(60);
        dto.OffsetCorrection.Should().Be(0.5);
        dto.GainCorrection.Should().Be(1.02);
        dto.LastCalibratedAt.Should().NotBeNull();
        dto.CalibrationNotes.Should().Be("Calibrated with reference thermometer");
        dto.Capabilities.Should().HaveCount(1);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateSensorDto_ShouldHaveRequiredAndOptionalValues()
    {
        // Act - v3.0: CreateSensorDto has Code, Name, Protocol, Category
        var dto = new CreateSensorDto(
            Code: "bme280-kitchen",
            Name: "Kitchen Sensor",
            Protocol: CommunicationProtocolDto.I2C,
            Category: "climate",
            Description: "Sensor in the kitchen",
            SerialNumber: "BME280-042"
        );

        // Assert
        dto.Code.Should().Be("bme280-kitchen");
        dto.Name.Should().Be("Kitchen Sensor");
        dto.Protocol.Should().Be(CommunicationProtocolDto.I2C);
        dto.Category.Should().Be("climate");
        dto.Description.Should().Be("Sensor in the kitchen");
        dto.SerialNumber.Should().Be("BME280-042");
    }

    [Fact]
    public void CreateSensorDto_ShouldAllowMinimalProperties()
    {
        // Act - v3.0: Only Code, Name, Protocol, Category required
        var dto = new CreateSensorDto(
            Code: "simple-sensor",
            Name: "Simple Sensor",
            Protocol: CommunicationProtocolDto.Analog,
            Category: "custom"
        );

        // Assert
        dto.Code.Should().Be("simple-sensor");
        dto.Name.Should().Be("Simple Sensor");
        dto.Description.Should().BeNull();
        dto.SerialNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateSensorDto_ShouldAllowPartialUpdates()
    {
        // Act - v3.0: UpdateSensorDto for updating sensor properties
        var dto = new UpdateSensorDto(
            Name: "Updated Name",
            IsActive: false
        );

        // Assert
        dto.Name.Should().Be("Updated Name");
        dto.IsActive.Should().BeFalse();
        dto.Description.Should().BeNull();
        dto.SerialNumber.Should().BeNull();
    }

    [Fact]
    public void CalibrateSensorDto_ShouldSetCalibrationValues()
    {
        // Act - CalibrateSensorDto for sensor calibration
        var dueDate = DateTime.UtcNow.AddMonths(6);
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.5,
            GainCorrection: 1.02,
            CalibrationNotes: "Calibrated with NIST-traceable reference",
            CalibrationDueAt: dueDate
        );

        // Assert
        dto.OffsetCorrection.Should().Be(0.5);
        dto.GainCorrection.Should().Be(1.02);
        dto.CalibrationNotes.Should().Be("Calibrated with NIST-traceable reference");
        dto.CalibrationDueAt.Should().Be(dueDate);
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
        // Arrange & Act - New model: Reading has AssignmentId, MeasurementType, RawValue, Value
        var assignmentId = Guid.NewGuid();
        var dto = new ReadingDto(
            Id: 1,
            TenantId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            NodeName: "Test Node",
            AssignmentId: assignmentId,
            SensorId: Guid.NewGuid(),
            SensorCode: "BME280",
            SensorName: "BME280 Temperature Sensor",
            SensorIcon: "thermostat",
            SensorColor: "#FF5722",
            MeasurementType: "temperature",
            DisplayName: "Temperature",
            RawValue: 21.3,
            Value: 21.5, // Calibrated
            Unit: "°C",
            Timestamp: DateTime.UtcNow,
            Location: new LocationDto("Living Room"),
            IsSyncedToCloud: false
        );

        // Assert
        dto.MeasurementType.Should().Be("temperature");
        dto.RawValue.Should().Be(21.3);
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.AssignmentId.Should().Be(assignmentId);
        dto.IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public void CreateReadingDto_ShouldHaveRequiredAndOptionalValues()
    {
        // Act - New model: CreateReadingDto has EndpointId, MeasurementType, RawValue
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 22.3
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.EndpointId.Should().Be(1);
        dto.MeasurementType.Should().Be("temperature");
        dto.RawValue.Should().Be(22.3);
        dto.HubId.Should().BeNull();
        dto.Timestamp.Should().BeNull();
    }

    [Fact]
    public void CreateReadingDto_WithAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act - New model with EndpointId and MeasurementType
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 2,
            MeasurementType: "humidity",
            RawValue: 65.5,
            HubId: "hub-01",
            Timestamp: timestamp
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.EndpointId.Should().Be(2);
        dto.MeasurementType.Should().Be("humidity");
        dto.RawValue.Should().Be(65.5);
        dto.HubId.Should().Be("hub-01");
        dto.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void ReadingFilterDto_ShouldHaveDefaultValues()
    {
        // Act - New model: ReadingFilterDto has MeasurementType and AssignmentId instead of SensorTypeId
        var dto = new ReadingFilterDto();

        // Assert
        dto.NodeId.Should().BeNull();
        dto.NodeIdentifier.Should().BeNull();
        dto.HubId.Should().BeNull();
        dto.AssignmentId.Should().BeNull();
        dto.MeasurementType.Should().BeNull();
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
        var assignmentId = Guid.NewGuid();
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        // Act - New model: MeasurementType and AssignmentId instead of SensorTypeId
        var dto = new ReadingFilterDto(
            NodeId: nodeId,
            NodeIdentifier: "node-01",
            HubId: hubId,
            AssignmentId: assignmentId,
            MeasurementType: "temperature",
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
        dto.AssignmentId.Should().Be(assignmentId);
        dto.MeasurementType.Should().Be("temperature");
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

#region SensorCapabilityDto Tests (v3.0 Two-Tier Model)

/// <summary>
/// Tests for SensorCapabilityDto (v3.0).
/// Capabilities now belong to Sensor directly, not SensorType.
/// </summary>
public class SensorCapabilityDtoTests
{
    [Fact]
    public void SensorCapabilityDto_ContainsMatterClusterInfo()
    {
        // Act - Capability contains Matter cluster info
        var capability = new SensorCapabilityDto(
            Id: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            MeasurementType: "temperature",
            DisplayName: "Temperature",
            Unit: "°C",
            MinValue: -40,
            MaxValue: 80,
            Resolution: 0.1,
            Accuracy: 0.5,
            MatterClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            SortOrder: 0,
            IsActive: true
        );

        // Assert
        capability.MatterClusterId.Should().Be(0x0402u);
        capability.MatterClusterName.Should().Be("TemperatureMeasurement");
        capability.Unit.Should().Be("°C");
        capability.MinValue.Should().Be(-40);
        capability.MaxValue.Should().Be(80);
    }

    [Fact]
    public void CreateSensorCapabilityDto_ShouldHaveRequiredAndOptionalValues()
    {
        // Act - v3.0: CreateSensorCapabilityDto
        var dto = new CreateSensorCapabilityDto(
            MeasurementType: "humidity",
            DisplayName: "Humidity",
            Unit: "%"
        );

        // Assert
        dto.MeasurementType.Should().Be("humidity");
        dto.DisplayName.Should().Be("Humidity");
        dto.Unit.Should().Be("%");
        dto.MinValue.Should().BeNull();
        dto.MaxValue.Should().BeNull();
        dto.Resolution.Should().Be(0.01);
        dto.Accuracy.Should().Be(0.5);
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

// Note: DefaultSensorTypes class was removed in the new 3-tier model.
// SensorTypes are now seeded via SensorTypeService.SeedDefaultTypesAsync()
// Tests for default sensor types are covered in SensorTypeServiceTests

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

#region SyncedNodeDto Tests

public class SyncedNodeDtoTests
{
    [Fact]
    public void SyncedNodeDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "dwd-cologne-01",
            Name: "DWD Köln Station",
            Source: SyncedNodeSourceDto.Virtual,
            SourceDetails: "DWD Station: 10513",
            Location: new LocationDto("Köln", 50.9375, 6.9603),
            IsOnline: true,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow.AddDays(-30)
        );

        // Assert
        dto.NodeId.Should().Be("dwd-cologne-01");
        dto.Name.Should().Be("DWD Köln Station");
        dto.Source.Should().Be(SyncedNodeSourceDto.Virtual);
        dto.SourceDetails.Should().Be("DWD Station: 10513");
        dto.Location.Should().NotBeNull();
        dto.Location!.Name.Should().Be("Köln");
        dto.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void SyncedNodeDto_ShouldAllowNullOptionalValues()
    {
        // Act
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "node-01",
            Name: "Test Node",
            Source: SyncedNodeSourceDto.Direct,
            SourceDetails: null,
            Location: null,
            IsOnline: false,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
    }

    [Fact]
    public void CreateSyncedNodeDto_ShouldHaveDefaultValues()
    {
        // Act
        var dto = new CreateSyncedNodeDto(
            CloudNodeId: Guid.NewGuid(),
            NodeId: "cloud-node-01",
            Name: "Cloud Node",
            Source: SyncedNodeSourceDto.OtherHub
        );

        // Assert
        dto.NodeId.Should().Be("cloud-node-01");
        dto.Name.Should().Be("Cloud Node");
        dto.Source.Should().Be(SyncedNodeSourceDto.OtherHub);
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void CreateSyncedNodeDto_ShouldAllowAllProperties()
    {
        // Act
        var location = new LocationDto("Berlin", 52.52, 13.405);
        var dto = new CreateSyncedNodeDto(
            CloudNodeId: Guid.NewGuid(),
            NodeId: "dwd-berlin-01",
            Name: "DWD Berlin",
            Source: SyncedNodeSourceDto.Virtual,
            SourceDetails: "DWD Station: 10382",
            Location: location,
            IsOnline: true
        );

        // Assert
        dto.SourceDetails.Should().Be("DWD Station: 10382");
        dto.Location.Should().Be(location);
        dto.IsOnline.Should().BeTrue();
    }

    [Theory]
    [InlineData(SyncedNodeSourceDto.Direct)]
    [InlineData(SyncedNodeSourceDto.Virtual)]
    [InlineData(SyncedNodeSourceDto.OtherHub)]
    public void SyncedNodeSourceDto_ShouldSupportAllValues(SyncedNodeSourceDto source)
    {
        // Act
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "test",
            Name: "Test",
            Source: source,
            SourceDetails: null,
            Location: null,
            IsOnline: false,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.Source.Should().Be(source);
    }
}

#endregion

#region SyncedReadingDto Tests

public class SyncedReadingDtoTests
{
    [Fact]
    public void SyncedReadingDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var syncedNodeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var syncedAt = DateTime.UtcNow;

        var dto = new SyncedReadingDto(
            Id: 12345,
            SyncedNodeId: syncedNodeId,
            SensorCode: "bme280",
            MeasurementType: "temperature",
            Value: 21.5,
            Unit: "°C",
            Timestamp: timestamp,
            SyncedAt: syncedAt
        );

        // Assert
        dto.Id.Should().Be(12345);
        dto.SyncedNodeId.Should().Be(syncedNodeId);
        dto.SensorCode.Should().Be("bme280");
        dto.MeasurementType.Should().Be("temperature");
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.Timestamp.Should().Be(timestamp);
        dto.SyncedAt.Should().Be(syncedAt);
    }

    [Fact]
    public void SyncedReadingDto_ShouldSupportLongId()
    {
        // Act - Testing long ID for time-series storage
        var dto = new SyncedReadingDto(
            Id: long.MaxValue,
            SyncedNodeId: Guid.NewGuid(),
            SensorCode: "test",
            MeasurementType: "test",
            Value: 0,
            Unit: "",
            Timestamp: DateTime.UtcNow,
            SyncedAt: DateTime.UtcNow
        );

        // Assert
        dto.Id.Should().Be(long.MaxValue);
    }

    [Fact]
    public void CreateSyncedReadingDto_ShouldHaveAllRequiredProperties()
    {
        // Arrange
        var syncedNodeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var dto = new CreateSyncedReadingDto(
            SyncedNodeId: syncedNodeId,
            SensorCode: "dht22",
            MeasurementType: "humidity",
            Value: 65.0,
            Unit: "%",
            Timestamp: timestamp
        );

        // Assert
        dto.SyncedNodeId.Should().Be(syncedNodeId);
        dto.SensorCode.Should().Be("dht22");
        dto.MeasurementType.Should().Be("humidity");
        dto.Value.Should().Be(65.0);
        dto.Unit.Should().Be("%");
        dto.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void SyncedReadingDto_ShouldSupportMultipleMeasurementTypes()
    {
        // Arrange & Act
        var syncedNodeId = Guid.NewGuid();
        var readings = new List<SyncedReadingDto>
        {
            new SyncedReadingDto(1, syncedNodeId, "bme280", "temperature", 21.5, "°C", DateTime.UtcNow, DateTime.UtcNow),
            new SyncedReadingDto(2, syncedNodeId, "bme280", "humidity", 65.0, "%", DateTime.UtcNow, DateTime.UtcNow),
            new SyncedReadingDto(3, syncedNodeId, "bme280", "pressure", 1013.25, "hPa", DateTime.UtcNow, DateTime.UtcNow)
        };

        // Assert
        readings.Should().HaveCount(3);
        readings.Select(r => r.MeasurementType).Should().Contain(new[] { "temperature", "humidity", "pressure" });
    }
}

#endregion

#region UnifiedNodeDto Tests

public class UnifiedNodeDtoTests
{
    [Fact]
    public void UnifiedNodeDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var sensors = new List<SensorDto>();
        var latestReadings = new List<UnifiedReadingDto>
        {
            new UnifiedReadingDto("temperature", "Temperature", 21.5, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.Local)
        };

        // Act
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "node-01",
            Name: "Living Room Node",
            Source: UnifiedNodeSourceDto.Local,
            SourceDetails: null,
            Sensors: sensors,
            Location: new LocationDto("Living Room"),
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            LatestReadings: latestReadings
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.Name.Should().Be("Living Room Node");
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
        dto.IsOnline.Should().BeTrue();
        dto.LatestReadings.Should().HaveCount(1);
    }

    [Fact]
    public void UnifiedNodeDto_ShouldAllowNullCollections()
    {
        // Act
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "node-02",
            Name: "Remote Node",
            Source: UnifiedNodeSourceDto.Virtual,
            SourceDetails: "External API",
            Sensors: null,
            Location: null,
            IsOnline: false,
            LastSeen: null,
            LatestReadings: null
        );

        // Assert
        dto.Sensors.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.LastSeen.Should().BeNull();
        dto.LatestReadings.Should().BeNull();
    }

    [Theory]
    [InlineData(UnifiedNodeSourceDto.Local)]
    [InlineData(UnifiedNodeSourceDto.Direct)]
    [InlineData(UnifiedNodeSourceDto.Virtual)]
    [InlineData(UnifiedNodeSourceDto.OtherHub)]
    public void UnifiedNodeSourceDto_ShouldSupportAllValues(UnifiedNodeSourceDto source)
    {
        // Act
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "test",
            Name: "Test",
            Source: source,
            SourceDetails: null,
            Sensors: null,
            Location: null,
            IsOnline: false,
            LastSeen: null,
            LatestReadings: null
        );

        // Assert
        dto.Source.Should().Be(source);
    }
}

#endregion

#region UnifiedReadingDto Tests

public class UnifiedReadingDtoTests
{
    [Fact]
    public void UnifiedReadingDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var dto = new UnifiedReadingDto(
            SensorTypeId: "temperature",
            SensorTypeName: "Temperature",
            Value: 21.5,
            Unit: "°C",
            Timestamp: timestamp,
            Source: UnifiedNodeSourceDto.Local
        );

        // Assert
        dto.SensorTypeId.Should().Be("temperature");
        dto.SensorTypeName.Should().Be("Temperature");
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.Timestamp.Should().Be(timestamp);
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
    }

    [Fact]
    public void UnifiedReadingDto_ShouldSupportDifferentSources()
    {
        // Act
        var readings = new List<UnifiedReadingDto>
        {
            new UnifiedReadingDto("temp", "Temperature", 21.0, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.Local),
            new UnifiedReadingDto("temp", "Temperature", 22.0, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.Direct),
            new UnifiedReadingDto("temp", "Temperature", 23.0, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.Virtual),
            new UnifiedReadingDto("temp", "Temperature", 24.0, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.OtherHub)
        };

        // Assert
        readings.Should().HaveCount(4);
        readings.Select(r => r.Source).Should().OnlyHaveUniqueItems();
    }
}

#endregion

#region HubStatusDto Tests

public class HubStatusDtoTestsNew
{
    [Fact]
    public void HubStatusDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var lastSeen = DateTime.UtcNow;
        var services = new ServiceStatusDto(
            Api: new ServiceState(true, "Running"),
            Database: new ServiceState(true, "Connected"),
            Mqtt: new ServiceState(true, "Connected"),
            Cloud: new ServiceState(false, "Not configured")
        );

        // Act
        var dto = new HubStatusDto(
            IsOnline: true,
            LastSeen: lastSeen,
            NodeCount: 5,
            OnlineNodeCount: 3,
            Services: services
        );

        // Assert
        dto.IsOnline.Should().BeTrue();
        dto.LastSeen.Should().Be(lastSeen);
        dto.NodeCount.Should().Be(5);
        dto.OnlineNodeCount.Should().Be(3);
        dto.Services.Should().NotBeNull();
        dto.Services.Api.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void HubStatusDto_ShouldAllowNullLastSeen()
    {
        // Act
        var services = new ServiceStatusDto(
            Api: new ServiceState(true),
            Database: new ServiceState(false, "Connection failed"),
            Mqtt: new ServiceState(false),
            Cloud: new ServiceState(false)
        );

        var dto = new HubStatusDto(
            IsOnline: false,
            LastSeen: null,
            NodeCount: 0,
            OnlineNodeCount: 0,
            Services: services
        );

        // Assert
        dto.IsOnline.Should().BeFalse();
        dto.LastSeen.Should().BeNull();
        dto.NodeCount.Should().Be(0);
    }
}

#endregion

#region ServiceStatusDto Tests

public class ServiceStatusDtoTestsNew
{
    [Fact]
    public void ServiceStatusDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var apiState = new ServiceState(true, "Running");
        var dbState = new ServiceState(true, "Connected");
        var mqttState = new ServiceState(false, "Not configured");
        var cloudState = new ServiceState(false, "Not connected");

        var dto = new ServiceStatusDto(
            Api: apiState,
            Database: dbState,
            Mqtt: mqttState,
            Cloud: cloudState
        );

        // Assert
        dto.Api.IsOnline.Should().BeTrue();
        dto.Api.Message.Should().Be("Running");
        dto.Database.IsOnline.Should().BeTrue();
        dto.Mqtt.IsOnline.Should().BeFalse();
        dto.Cloud.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void ServiceState_ShouldBeCreatedWithOnlineStatus()
    {
        // Act
        var state = new ServiceState(true, "Healthy");

        // Assert
        state.IsOnline.Should().BeTrue();
        state.Message.Should().Be("Healthy");
    }

    [Fact]
    public void ServiceState_ShouldAllowNullMessage()
    {
        // Act
        var state = new ServiceState(false);

        // Assert
        state.IsOnline.Should().BeFalse();
        state.Message.Should().BeNull();
    }
}

#endregion

#region NodeSensorAssignmentDto Additional Tests

public class NodeSensorAssignmentDtoAdditionalTests
{
    [Fact]
    public void NodeSensorAssignmentDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var effectiveConfig = new EffectiveConfigDto(
            IntervalSeconds: 60,
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            BaudRate: null,
            OffsetCorrection: 0,
            GainCorrection: 1
        );

        // Act
        var dto = new NodeSensorAssignmentDto(
            Id: assignmentId,
            NodeId: nodeId,
            NodeName: "Test Node",
            SensorId: sensorId,
            SensorCode: "BME280",
            SensorName: "BME280 Sensor",
            EndpointId: 1,
            Alias: "Living Room Temp",
            I2CAddressOverride: "0x77",
            SdaPinOverride: null,
            SclPinOverride: null,
            OneWirePinOverride: null,
            AnalogPinOverride: null,
            DigitalPinOverride: null,
            TriggerPinOverride: null,
            EchoPinOverride: null,
            BaudRateOverride: null,
            IntervalSecondsOverride: 30,
            IsActive: true,
            LastSeenAt: DateTime.UtcNow,
            AssignedAt: DateTime.UtcNow.AddDays(-7),
            EffectiveConfig: effectiveConfig
        );

        // Assert
        dto.Id.Should().Be(assignmentId);
        dto.NodeId.Should().Be(nodeId);
        dto.NodeName.Should().Be("Test Node");
        dto.SensorCode.Should().Be("BME280");
        dto.EndpointId.Should().Be(1);
        dto.Alias.Should().Be("Living Room Temp");
        dto.I2CAddressOverride.Should().Be("0x77");
        dto.IntervalSecondsOverride.Should().Be(30);
        dto.IsActive.Should().BeTrue();
        dto.EffectiveConfig.Should().NotBeNull();
    }

    [Fact]
    public void NodeSensorAssignmentDto_ShouldAllowNullOptionalValues()
    {
        // Act
        var effectiveConfig = new EffectiveConfigDto(60, null, null, null, null, null, null, null, null, null, 0, 1);
        var dto = new NodeSensorAssignmentDto(
            Id: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            NodeName: "Node",
            SensorId: Guid.NewGuid(),
            SensorCode: "DHT22",
            SensorName: "DHT22 Sensor",
            EndpointId: 0,
            Alias: null,
            I2CAddressOverride: null,
            SdaPinOverride: null,
            SclPinOverride: null,
            OneWirePinOverride: null,
            AnalogPinOverride: null,
            DigitalPinOverride: null,
            TriggerPinOverride: null,
            EchoPinOverride: null,
            BaudRateOverride: null,
            IntervalSecondsOverride: null,
            IsActive: false,
            LastSeenAt: null,
            AssignedAt: DateTime.UtcNow,
            EffectiveConfig: effectiveConfig
        );

        // Assert
        dto.Alias.Should().BeNull();
        dto.I2CAddressOverride.Should().BeNull();
        dto.IntervalSecondsOverride.Should().BeNull();
        dto.LastSeenAt.Should().BeNull();
    }
}

#endregion

#region MqttTopics Tests

public class MqttTopicsTests
{
    [Fact]
    public void MqttTopics_Prefix_ShouldBeCorrect()
    {
        // Assert
        MqttTopics.Prefix.Should().Be("myiotgrid");
    }

    [Fact]
    public void MqttTopics_GetSensorDataTopic_ShouldFormatCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var topic = MqttTopics.GetSensorDataTopic(tenantId);

        // Assert
        topic.Should().StartWith("myiotgrid/");
        topic.Should().EndWith("/sensordata");
    }

    [Fact]
    public void MqttTopics_GetHubStatusTopic_ShouldFormatCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var hubId = "hub-01";

        // Act
        var topic = MqttTopics.GetHubStatusTopic(tenantId, hubId);

        // Assert
        topic.Should().StartWith("myiotgrid/");
        topic.Should().Contain("/hubs/");
        topic.Should().EndWith("/status");
    }

    [Fact]
    public void MqttTopics_GetAlertsTopic_ShouldFormatCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var topic = MqttTopics.GetAlertsTopic(tenantId);

        // Assert
        topic.Should().StartWith("myiotgrid/");
        topic.Should().EndWith("/alerts");
    }

    [Fact]
    public void MqttTopics_GetSensorDataWildcard_ShouldFormatCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var topic = MqttTopics.GetSensorDataWildcard(tenantId);

        // Assert
        topic.Should().Be($"myiotgrid/{tenantId}/sensordata");
    }

    [Fact]
    public void MqttTopics_GetHubStatusWildcard_ShouldFormatCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var topic = MqttTopics.GetHubStatusWildcard(tenantId);

        // Assert
        topic.Should().Be($"myiotgrid/{tenantId}/hubs/+/status");
    }

    [Fact]
    public void MqttTopics_ChirpStackUplink_ShouldBeCorrect()
    {
        // Assert
        MqttTopics.ChirpStackUplink.Should().Be("application/+/device/+/event/up");
    }
}

#endregion

#region NodeRegistrationResponseDto Tests

public class NodeRegistrationResponseDtoTests
{
    [Fact]
    public void NodeRegistrationResponseDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensors = new List<SensorConfigDto>
        {
            new SensorConfigDto("temperature", true, 21),
            new SensorConfigDto("humidity", true, 22)
        };
        var connection = new ConnectionConfigDto("REST", "https://hub.local/api");

        // Act
        var dto = new NodeRegistrationResponseDto(
            NodeId: nodeId,
            SerialNumber: "SN123456",
            Name: "Living Room Sensor",
            Location: "Living Room",
            IntervalSeconds: 60,
            Sensors: sensors,
            Connection: connection,
            IsNewNode: true,
            Message: "Node registered successfully"
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.SerialNumber.Should().Be("SN123456");
        dto.Name.Should().Be("Living Room Sensor");
        dto.Location.Should().Be("Living Room");
        dto.IntervalSeconds.Should().Be(60);
        dto.Sensors.Should().HaveCount(2);
        dto.Connection.Mode.Should().Be("REST");
        dto.IsNewNode.Should().BeTrue();
        dto.Message.Should().Be("Node registered successfully");
    }

    [Fact]
    public void NodeRegistrationResponseDto_ShouldAllowNullLocation()
    {
        // Act
        var dto = new NodeRegistrationResponseDto(
            NodeId: Guid.NewGuid(),
            SerialNumber: "SN000",
            Name: "Sensor",
            Location: null,
            IntervalSeconds: 30,
            Sensors: new List<SensorConfigDto>(),
            Connection: new ConnectionConfigDto("MQTT", "mqtt://broker"),
            IsNewNode: false,
            Message: "OK"
        );

        // Assert
        dto.Location.Should().BeNull();
    }
}

#endregion

#region SensorConfigDto Tests

public class SensorConfigDtoTests
{
    [Fact]
    public void SensorConfigDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new SensorConfigDto("temperature", true, 21);

        // Assert
        dto.Type.Should().Be("temperature");
        dto.Enabled.Should().BeTrue();
        dto.Pin.Should().Be(21);
    }

    [Fact]
    public void SensorConfigDto_ShouldHaveDefaultPinValue()
    {
        // Act
        var dto = new SensorConfigDto("humidity", false);

        // Assert
        dto.Type.Should().Be("humidity");
        dto.Enabled.Should().BeFalse();
        dto.Pin.Should().Be(-1);
    }
}

#endregion

#region ConnectionConfigDto Tests

public class ConnectionConfigDtoTests
{
    [Fact]
    public void ConnectionConfigDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new ConnectionConfigDto("REST", "https://hub.local/api");

        // Assert
        dto.Mode.Should().Be("REST");
        dto.Endpoint.Should().Be("https://hub.local/api");
    }

    [Fact]
    public void ConnectionConfigDto_ShouldSupportMqttMode()
    {
        // Act
        var dto = new ConnectionConfigDto("MQTT", "mqtt://broker:1883");

        // Assert
        dto.Mode.Should().Be("MQTT");
        dto.Endpoint.Should().Be("mqtt://broker:1883");
    }
}

#endregion

#region NodeSensorConfigurationDto Tests

public class NodeSensorConfigurationDtoTests
{
    [Fact]
    public void NodeSensorConfigurationDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var capabilities = new List<SensorCapabilityConfigDto>
        {
            new("temperature", "Temperatur", "°C"),
            new("humidity", "Luftfeuchtigkeit", "%"),
            new("pressure", "Luftdruck", "hPa")
        };
        var sensors = new List<SensorAssignmentConfigDto>
        {
            new SensorAssignmentConfigDto(
                EndpointId: 1,
                SensorCode: "BME280",
                SensorName: "BME280 Sensor",
                Icon: "thermostat",
                Color: "#FF5722",
                IsActive: true,
                IntervalSeconds: 60,
                I2CAddress: "0x76",
                SdaPin: 21,
                SclPin: 22,
                OneWirePin: null,
                AnalogPin: null,
                DigitalPin: null,
                TriggerPin: null,
                EchoPin: null,
                OffsetCorrection: 0.1,
                GainCorrection: 1.02,
                Capabilities: capabilities
            )
        };
        var configTimestamp = DateTime.UtcNow;

        // Act
        var dto = new NodeSensorConfigurationDto(
            NodeId: nodeId,
            SerialNumber: "SIM-AA:BB:CC",
            Name: "Test Node",
            IsSimulation: true,
            DefaultIntervalSeconds: 60,
            Sensors: sensors,
            ConfigurationTimestamp: configTimestamp
        );

        // Assert
        dto.NodeId.Should().Be(nodeId);
        dto.SerialNumber.Should().Be("SIM-AA:BB:CC");
        dto.Name.Should().Be("Test Node");
        dto.IsSimulation.Should().BeTrue();
        dto.DefaultIntervalSeconds.Should().Be(60);
        dto.Sensors.Should().HaveCount(1);
        dto.Sensors[0].Capabilities.Should().HaveCount(3);
        dto.ConfigurationTimestamp.Should().Be(configTimestamp);
    }
}

#endregion

#region SensorAssignmentConfigDto Tests

public class SensorAssignmentConfigDtoTests
{
    [Fact]
    public void SensorAssignmentConfigDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var capabilities = new List<SensorCapabilityConfigDto>
        {
            new("temperature", "Temperatur", "°C"),
            new("humidity", "Luftfeuchtigkeit", "%")
        };

        var dto = new SensorAssignmentConfigDto(
            EndpointId: 1,
            SensorCode: "DHT22",
            SensorName: "DHT22 Temperature/Humidity",
            Icon: "thermostat",
            Color: "#4CAF50",
            IsActive: true,
            IntervalSeconds: 30,
            I2CAddress: null,
            SdaPin: null,
            SclPin: null,
            OneWirePin: 4,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            OffsetCorrection: -0.5,
            GainCorrection: 1.0,
            Capabilities: capabilities
        );

        // Assert
        dto.EndpointId.Should().Be(1);
        dto.SensorCode.Should().Be("DHT22");
        dto.SensorName.Should().Be("DHT22 Temperature/Humidity");
        dto.Icon.Should().Be("thermostat");
        dto.Color.Should().Be("#4CAF50");
        dto.IsActive.Should().BeTrue();
        dto.IntervalSeconds.Should().Be(30);
        dto.I2CAddress.Should().BeNull();
        dto.OneWirePin.Should().Be(4);
        dto.OffsetCorrection.Should().Be(-0.5);
        dto.GainCorrection.Should().Be(1.0);
        dto.Capabilities.Should().HaveCount(2);
        dto.Capabilities[0].MeasurementType.Should().Be("temperature");
        dto.Capabilities[0].Unit.Should().Be("°C");
    }

    [Fact]
    public void SensorAssignmentConfigDto_ShouldSupportI2CSensor()
    {
        // Act
        var capabilities = new List<SensorCapabilityConfigDto>
        {
            new("temperature", "Temperatur", "°C"),
            new("humidity", "Luftfeuchtigkeit", "%"),
            new("pressure", "Luftdruck", "hPa")
        };

        var dto = new SensorAssignmentConfigDto(
            EndpointId: 2,
            SensorCode: "BME280",
            SensorName: "BME280",
            Icon: null,
            Color: null,
            IsActive: true,
            IntervalSeconds: 60,
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            OffsetCorrection: 0,
            GainCorrection: 1,
            Capabilities: capabilities
        );

        // Assert
        dto.I2CAddress.Should().Be("0x76");
        dto.SdaPin.Should().Be(21);
        dto.SclPin.Should().Be(22);
        dto.Capabilities.Should().HaveCount(3);
    }
}

#endregion

#region ReadingValueDto Tests

public class ReadingValueDtoTests
{
    [Fact]
    public void ReadingValueDto_ShouldBeCreatedWithAllProperties()
    {
        // Act - ReadingValueDto has: EndpointId, MeasurementType, RawValue
        var dto = new ReadingValueDto(
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5
        );

        // Assert
        dto.EndpointId.Should().Be(1);
        dto.MeasurementType.Should().Be("temperature");
        dto.RawValue.Should().Be(21.5);
    }

    [Fact]
    public void ReadingValueDto_ShouldSupportDifferentEndpoints()
    {
        // Act
        var dto1 = new ReadingValueDto(EndpointId: 1, MeasurementType: "temperature", RawValue: 21.5);
        var dto2 = new ReadingValueDto(EndpointId: 2, MeasurementType: "humidity", RawValue: 65.0);

        // Assert
        dto1.EndpointId.Should().Be(1);
        dto2.EndpointId.Should().Be(2);
    }
}

#endregion

#region CreateBatchReadingsDto Tests

public class CreateBatchReadingsDtoTests
{
    [Fact]
    public void CreateBatchReadingsDto_ShouldBeCreatedWithReadings()
    {
        // Arrange - CreateBatchReadingsDto has: NodeId, HubId?, Readings, Timestamp?
        var readings = new List<ReadingValueDto>
        {
            new ReadingValueDto(EndpointId: 1, MeasurementType: "temperature", RawValue: 21.5),
            new ReadingValueDto(EndpointId: 1, MeasurementType: "humidity", RawValue: 65.0)
        };

        // Act
        var dto = new CreateBatchReadingsDto(
            NodeId: "node-01",
            HubId: "hub-01",
            Readings: readings,
            Timestamp: DateTime.UtcNow
        );

        // Assert
        dto.NodeId.Should().Be("node-01");
        dto.HubId.Should().Be("hub-01");
        dto.Readings.Should().HaveCount(2);
        dto.Readings.First().MeasurementType.Should().Be("temperature");
        dto.Readings.Last().MeasurementType.Should().Be("humidity");
    }

    [Fact]
    public void CreateBatchReadingsDto_ShouldAllowNullOptionalFields()
    {
        // Act
        var dto = new CreateBatchReadingsDto(
            NodeId: "node-01",
            HubId: null,
            Readings: new List<ReadingValueDto>(),
            Timestamp: null
        );

        // Assert
        dto.HubId.Should().BeNull();
        dto.Timestamp.Should().BeNull();
        dto.Readings.Should().BeEmpty();
    }
}

#endregion

#region LatestMeasurementDto Tests

public class LatestMeasurementDtoTests
{
    [Fact]
    public void LatestMeasurementDto_ShouldBeCreatedWithAllProperties()
    {
        // Act - LatestMeasurementDto has: ReadingId, MeasurementType, DisplayName, RawValue, Value, Unit, Timestamp
        var timestamp = DateTime.UtcNow;
        var dto = new LatestMeasurementDto(
            ReadingId: 12345,
            MeasurementType: "temperature",
            DisplayName: "Temperatur",
            RawValue: 21.3,
            Value: 21.5,
            Unit: "°C",
            Timestamp: timestamp
        );

        // Assert
        dto.ReadingId.Should().Be(12345);
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperatur");
        dto.RawValue.Should().Be(21.3);
        dto.Value.Should().Be(21.5);
        dto.Unit.Should().Be("°C");
        dto.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void LatestMeasurementDto_ShouldSupportDifferentMeasurementTypes()
    {
        // Act
        var tempDto = new LatestMeasurementDto(
            ReadingId: 1,
            MeasurementType: "temperature",
            DisplayName: "Temperature",
            RawValue: 21.5,
            Value: 21.5,
            Unit: "°C",
            Timestamp: DateTime.UtcNow
        );

        var humidDto = new LatestMeasurementDto(
            ReadingId: 2,
            MeasurementType: "humidity",
            DisplayName: "Humidity",
            RawValue: 65.0,
            Value: 65.0,
            Unit: "%",
            Timestamp: DateTime.UtcNow
        );

        // Assert
        tempDto.MeasurementType.Should().Be("temperature");
        tempDto.Unit.Should().Be("°C");
        humidDto.MeasurementType.Should().Be("humidity");
        humidDto.Unit.Should().Be("%");
    }
}

#endregion

#region PagedResultDto Additional Tests

public class PagedResultDtoExtendedTests
{
    [Fact]
    public void PagedResultDto_HasNextPage_ReturnsTrueWhenNotOnLastPage()
    {
        // Act
        var dto = new PagedResultDto<string>
        {
            Items = new List<string> { "A", "B", "C" },
            TotalRecords = 30,
            Page = 0,
            Size = 10
        };

        // Assert
        dto.HasNextPage.Should().BeTrue();
        dto.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedResultDto_HasNextPage_ReturnsFalseOnLastPage()
    {
        // Act
        var dto = new PagedResultDto<string>
        {
            Items = new List<string> { "A", "B", "C" },
            TotalRecords = 30,
            Page = 2,
            Size = 10
        };

        // Assert
        dto.HasNextPage.Should().BeFalse();
        dto.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResultDto_Create_ReturnsCorrectResult()
    {
        // Arrange
        var items = new List<string> { "A", "B", "C" };
        var queryParams = new QueryParamsDto { Page = 1, Size = 10 };

        // Act
        var dto = PagedResultDto<string>.Create(items, 30, queryParams);

        // Assert
        dto.Items.Should().BeEquivalentTo(items);
        dto.TotalRecords.Should().Be(30);
        dto.Page.Should().Be(1);
        dto.Size.Should().Be(10);
        dto.TotalPages.Should().Be(3);
    }

    [Fact]
    public void PagedResultDto_Empty_ReturnsEmptyResult()
    {
        // Arrange
        var queryParams = new QueryParamsDto { Page = 0, Size = 10 };

        // Act
        var dto = PagedResultDto<string>.Empty(queryParams);

        // Assert
        dto.Items.Should().BeEmpty();
        dto.TotalRecords.Should().Be(0);
        dto.TotalPages.Should().Be(0);
    }

    [Fact]
    public void PagedResultDto_WithZeroSize_HandlesGracefully()
    {
        // Act
        var dto = new PagedResultDto<string>
        {
            Items = new List<string>(),
            TotalRecords = 0,
            Page = 0,
            Size = 0
        };

        // Assert
        dto.TotalPages.Should().Be(0); // Avoids division by zero
    }
}

#endregion

#region SensorCapabilityDto Extended Tests

public class SensorCapabilityDtoExtendedTests
{
    [Fact]
    public void SensorCapabilityDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new SensorCapabilityDto(
            Id: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            MeasurementType: "temperature",
            DisplayName: "Temperatur",
            Unit: "°C",
            MinValue: -40,
            MaxValue: 85,
            Resolution: 0.1,
            Accuracy: 0.5,
            MatterClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            SortOrder: 1,
            IsActive: true
        );

        // Assert
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperatur");
        dto.Unit.Should().Be("°C");
        dto.MinValue.Should().Be(-40);
        dto.MaxValue.Should().Be(85);
        dto.Resolution.Should().Be(0.1);
        dto.Accuracy.Should().Be(0.5);
        dto.MatterClusterId.Should().Be(0x0402u);
        dto.MatterClusterName.Should().Be("TemperatureMeasurement");
        dto.SortOrder.Should().Be(1);
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void SensorCapabilityDto_ShouldAllowNullOptionalFields()
    {
        // Act
        var dto = new SensorCapabilityDto(
            Id: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            MeasurementType: "custom",
            DisplayName: "Custom Sensor",
            Unit: "",
            MinValue: null,
            MaxValue: null,
            Resolution: 1,
            Accuracy: 0,
            MatterClusterId: null,
            MatterClusterName: null,
            SortOrder: 0,
            IsActive: false
        );

        // Assert
        dto.MinValue.Should().BeNull();
        dto.MaxValue.Should().BeNull();
        dto.MatterClusterId.Should().BeNull();
        dto.MatterClusterName.Should().BeNull();
        dto.IsActive.Should().BeFalse();
    }
}

#endregion

#region SensorDto Extended Tests

public class SensorDtoExtendedTests
{
    [Fact]
    public void SensorDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var capabilities = new List<SensorCapabilityDto>
        {
            new SensorCapabilityDto(
                Id: Guid.NewGuid(), SensorId: Guid.NewGuid(),
                MeasurementType: "temperature", DisplayName: "Temp", Unit: "°C",
                MinValue: null, MaxValue: null, Resolution: 0.1, Accuracy: 0.5,
                MatterClusterId: null, MatterClusterName: null, SortOrder: 0, IsActive: true)
        };

        var dto = new SensorDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            Code: "BME280",
            Name: "BME280 Environmental Sensor",
            Description: "Temperature, Humidity, Pressure sensor",
            SerialNumber: "SN123456",
            Manufacturer: "Bosch",
            Model: "BME280",
            DatasheetUrl: "https://example.com/datasheet",
            Protocol: CommunicationProtocolDto.I2C,
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            BaudRate: null,
            IntervalSeconds: 60,
            MinIntervalSeconds: 10,
            WarmupTimeMs: 100,
            OffsetCorrection: 0.5,
            GainCorrection: 1.0,
            LastCalibratedAt: DateTime.UtcNow,
            CalibrationNotes: "Calibrated against reference",
            CalibrationDueAt: DateTime.UtcNow.AddDays(365),
            Category: "environmental",
            Icon: "thermostat",
            Color: "#FF5722",
            Capabilities: capabilities,
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow
        );

        // Assert
        dto.Code.Should().Be("BME280");
        dto.Name.Should().Be("BME280 Environmental Sensor");
        dto.Protocol.Should().Be(CommunicationProtocolDto.I2C);
        dto.I2CAddress.Should().Be("0x76");
        dto.SdaPin.Should().Be(21);
        dto.SclPin.Should().Be(22);
        dto.Capabilities.Should().HaveCount(1);
        dto.CalibrationNotes.Should().Be("Calibrated against reference");
    }
}

#endregion

#region NodeSensorAssignmentDto Extended Tests

public class NodeSensorAssignmentDtoExtendedTests
{
    [Fact]
    public void NodeSensorAssignmentDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var effectiveConfig = new EffectiveConfigDto(
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            BaudRate: null,
            IntervalSeconds: 120,
            OffsetCorrection: 0.5,
            GainCorrection: 1.02
        );

        var dto = new NodeSensorAssignmentDto(
            Id: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            NodeName: "Living Room Node",
            SensorId: Guid.NewGuid(),
            SensorCode: "BME280",
            SensorName: "BME280 Sensor",
            EndpointId: 1,
            Alias: "Living Room Temperature",
            I2CAddressOverride: "0x77",
            SdaPinOverride: 19,
            SclPinOverride: 20,
            OneWirePinOverride: null,
            AnalogPinOverride: null,
            DigitalPinOverride: null,
            TriggerPinOverride: null,
            EchoPinOverride: null,
            BaudRateOverride: null,
            IntervalSecondsOverride: 120,
            IsActive: true,
            LastSeenAt: DateTime.UtcNow,
            AssignedAt: DateTime.UtcNow,
            EffectiveConfig: effectiveConfig
        );

        // Assert
        dto.NodeName.Should().Be("Living Room Node");
        dto.SensorCode.Should().Be("BME280");
        dto.Alias.Should().Be("Living Room Temperature");
        dto.I2CAddressOverride.Should().Be("0x77");
        dto.SdaPinOverride.Should().Be(19);
        dto.SclPinOverride.Should().Be(20);
        dto.IntervalSecondsOverride.Should().Be(120);
        dto.EffectiveConfig.Should().NotBeNull();
        dto.EffectiveConfig.I2CAddress.Should().Be("0x76");
    }
}

#endregion

#region AlertDto Extended Tests

public class AlertDtoExtendedTests
{
    [Fact]
    public void AlertDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new AlertDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            HubName: "Main Hub",
            NodeId: Guid.NewGuid(),
            NodeName: "Living Room Sensor",
            AlertTypeId: Guid.NewGuid(),
            AlertTypeCode: "mold_risk",
            AlertTypeName: "Mold Risk Warning",
            Level: AlertLevelDto.Warning,
            Message: "Elevated mold risk detected",
            Recommendation: "Improve ventilation",
            Source: AlertSourceDto.Cloud,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddDays(7),
            AcknowledgedAt: DateTime.UtcNow.AddHours(1),
            IsActive: true
        );

        // Assert
        dto.HubName.Should().Be("Main Hub");
        dto.NodeName.Should().Be("Living Room Sensor");
        dto.AlertTypeCode.Should().Be("mold_risk");
        dto.Level.Should().Be(AlertLevelDto.Warning);
        dto.Source.Should().Be(AlertSourceDto.Cloud);
        dto.ExpiresAt.Should().NotBeNull();
        dto.AcknowledgedAt.Should().NotBeNull();
    }
}

#endregion

#region NodeDto Extended Tests

public class NodeDtoExtendedTests
{
    [Fact]
    public void NodeDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var location = new LocationDto("Living Room", 50.9375, 6.9603);
        var dto = new NodeDto(
            Id: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            NodeId: "node-living-room-01",
            Name: "Living Room Sensor",
            Protocol: ProtocolDto.WLAN,
            Location: location,
            AssignmentCount: 3,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: "1.2.3",
            BatteryLevel: 85,
            CreatedAt: DateTime.UtcNow,
            MacAddress: "AA:BB:CC:DD:EE:FF",
            Status: NodeProvisioningStatusDto.Configured,
            IsSimulation: false,
            StorageMode: StorageModeDto.RemoteOnly,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null,
            DebugLevel: DebugLevelDto.Normal,
            EnableRemoteLogging: false,
            LastDebugChange: null
        );

        // Assert
        dto.NodeId.Should().Be("node-living-room-01");
        dto.Name.Should().Be("Living Room Sensor");
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.Location.Should().NotBeNull();
        dto.Location!.Name.Should().Be("Living Room");
        dto.AssignmentCount.Should().Be(3);
        dto.FirmwareVersion.Should().Be("1.2.3");
        dto.BatteryLevel.Should().Be(85);
        dto.MacAddress.Should().Be("AA:BB:CC:DD:EE:FF");
        dto.Status.Should().Be(NodeProvisioningStatusDto.Configured);
        dto.IsSimulation.Should().BeFalse();
    }
}

#endregion

#region SyncedNodeDto Extended Tests

public class SyncedNodeDtoExtendedTests
{
    [Fact]
    public void SyncedNodeDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var location = new LocationDto("Cologne", 50.9375, 6.9603);
        var now = DateTime.UtcNow;
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "dwd-cologne-01",
            Name: "DWD Cologne Station",
            Source: SyncedNodeSourceDto.Virtual,
            SourceDetails: "DWD Weather Service",
            Location: location,
            IsOnline: true,
            LastSyncAt: now,
            CreatedAt: now
        );

        // Assert
        dto.NodeId.Should().Be("dwd-cologne-01");
        dto.Name.Should().Be("DWD Cologne Station");
        dto.Source.Should().Be(SyncedNodeSourceDto.Virtual);
        dto.SourceDetails.Should().Be("DWD Weather Service");
        dto.Location.Should().NotBeNull();
        dto.IsOnline.Should().BeTrue();
        dto.LastSyncAt.Should().Be(now);
        dto.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void SyncedNodeDto_ShouldHandleNullableFields()
    {
        // Act
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "direct-node-01",
            Name: "Direct Node",
            Source: SyncedNodeSourceDto.Direct,
            SourceDetails: null,
            Location: null,
            IsOnline: false,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.IsOnline.Should().BeFalse();
    }

    [Theory]
    [InlineData(SyncedNodeSourceDto.Direct)]
    [InlineData(SyncedNodeSourceDto.Virtual)]
    [InlineData(SyncedNodeSourceDto.OtherHub)]
    public void SyncedNodeDto_ShouldSupportAllSourceTypes(SyncedNodeSourceDto source)
    {
        // Act
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: $"node-{source}",
            Name: $"Node {source}",
            Source: source,
            SourceDetails: null,
            Location: null,
            IsOnline: true,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );

        // Assert
        dto.Source.Should().Be(source);
    }
}

#endregion

#region TenantDto Extended Tests

public class TenantDtoExtendedTests
{
    [Fact]
    public void TenantDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new TenantDto(
            Id: Guid.NewGuid(),
            Name: "Test Tenant",
            CloudApiKey: "api-key-12345",
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            LastSyncAt: DateTime.UtcNow
        );

        // Assert
        dto.Name.Should().Be("Test Tenant");
        dto.CloudApiKey.Should().Be("api-key-12345");
        dto.IsActive.Should().BeTrue();
        dto.LastSyncAt.Should().NotBeNull();
    }
}

#endregion

#region ReadingDto Extended Tests

public class ReadingDtoExtendedTests
{
    [Fact]
    public void ReadingDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var location = new LocationDto("Living Room", 50.9375, 6.9603);

        // Act
        var dto = new ReadingDto(
            Id: 12345,
            TenantId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            NodeName: "Living Room Node",
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            SensorCode: "BME280",
            SensorName: "BME280 Sensor",
            SensorIcon: "thermostat",
            SensorColor: "#FF5722",
            MeasurementType: "temperature",
            DisplayName: "Temperatur",
            RawValue: 21.3,
            Value: 21.5,
            Unit: "°C",
            Timestamp: timestamp,
            Location: location,
            IsSyncedToCloud: true
        );

        // Assert
        dto.Id.Should().Be(12345);
        dto.NodeName.Should().Be("Living Room Node");
        dto.SensorCode.Should().Be("BME280");
        dto.SensorIcon.Should().Be("thermostat");
        dto.SensorColor.Should().Be("#FF5722");
        dto.RawValue.Should().Be(21.3);
        dto.Value.Should().Be(21.5);
        dto.Location.Should().NotBeNull();
        dto.IsSyncedToCloud.Should().BeTrue();
    }
}

#endregion

#region HubDto Extended Tests

public class HubDtoExtendedTests
{
    [Fact]
    public void HubDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new HubDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: "hub-main-01",
            Name: "Main Hub",
            Description: "Primary hub for all sensors",
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 10
        );

        // Assert
        dto.HubId.Should().Be("hub-main-01");
        dto.Name.Should().Be("Main Hub");
        dto.Description.Should().Be("Primary hub for all sensors");
        dto.IsOnline.Should().BeTrue();
        dto.SensorCount.Should().Be(10);
    }
}

#endregion

#region UnifiedNodeDto Extended Tests

public class UnifiedNodeDtoExtendedTests
{
    [Fact]
    public void UnifiedNodeDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange
        var location = new LocationDto("Outdoor", 50.9375, 6.9603);
        var latestReadings = new List<UnifiedReadingDto>
        {
            new UnifiedReadingDto(
                SensorTypeId: "temperature",
                SensorTypeName: "Temperature",
                Value: 21.5,
                Unit: "°C",
                Timestamp: DateTime.UtcNow,
                Source: UnifiedNodeSourceDto.Local
            )
        };

        // Act
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "unified-node-01",
            Name: "Unified Node",
            Source: UnifiedNodeSourceDto.Local,
            SourceDetails: "Local hub connection",
            Sensors: null,  // Sensors is nullable and has complex structure
            Location: location,
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            LatestReadings: latestReadings
        );

        // Assert
        dto.NodeId.Should().Be("unified-node-01");
        dto.Name.Should().Be("Unified Node");
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
        dto.SourceDetails.Should().Be("Local hub connection");
        dto.Location.Should().NotBeNull();
        dto.Location!.Name.Should().Be("Outdoor");
        dto.LatestReadings.Should().HaveCount(1);
        dto.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void UnifiedReadingDto_ShouldBeCreatedCorrectly()
    {
        // Act
        var now = DateTime.UtcNow;
        var dto = new UnifiedReadingDto(
            SensorTypeId: "humidity",
            SensorTypeName: "Humidity",
            Value: 65.5,
            Unit: "%",
            Timestamp: now,
            Source: UnifiedNodeSourceDto.Virtual
        );

        // Assert
        dto.SensorTypeId.Should().Be("humidity");
        dto.SensorTypeName.Should().Be("Humidity");
        dto.Value.Should().Be(65.5);
        dto.Unit.Should().Be("%");
        dto.Timestamp.Should().Be(now);
        dto.Source.Should().Be(UnifiedNodeSourceDto.Virtual);
    }

    [Fact]
    public void UnifiedNodeDto_ShouldHandleNullableFields()
    {
        // Act
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "minimal-node",
            Name: "Minimal Node",
            Source: UnifiedNodeSourceDto.Virtual,
            SourceDetails: null,
            Sensors: null,
            Location: null,
            IsOnline: false,
            LastSeen: null,
            LatestReadings: null
        );

        // Assert
        dto.SourceDetails.Should().BeNull();
        dto.Location.Should().BeNull();
        dto.Sensors.Should().BeNull();
        dto.LatestReadings.Should().BeNull();
        dto.LastSeen.Should().BeNull();
        dto.IsOnline.Should().BeFalse();
    }

    [Theory]
    [InlineData(UnifiedNodeSourceDto.Local)]
    [InlineData(UnifiedNodeSourceDto.Direct)]
    [InlineData(UnifiedNodeSourceDto.Virtual)]
    [InlineData(UnifiedNodeSourceDto.OtherHub)]
    public void UnifiedNodeDto_ShouldSupportAllSourceTypes(UnifiedNodeSourceDto source)
    {
        // Act
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: $"node-{source}",
            Name: $"Node {source}",
            Source: source,
            SourceDetails: null,
            Sensors: null,
            Location: null,
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            LatestReadings: null
        );

        // Assert
        dto.Source.Should().Be(source);
    }
}

#endregion

#region LocationDto Extended Tests

public class LocationDtoExtendedTests
{
    [Fact]
    public void LocationDto_ShouldBeCreatedWithAllProperties()
    {
        // Act
        var dto = new LocationDto("Living Room", 50.9375, 6.9603);

        // Assert
        dto.Name.Should().Be("Living Room");
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void LocationDto_ShouldAllowPartialData()
    {
        // Act - Name only
        var nameOnly = new LocationDto("Kitchen", null, null);

        // Assert
        nameOnly.Name.Should().Be("Kitchen");
        nameOnly.Latitude.Should().BeNull();
        nameOnly.Longitude.Should().BeNull();
    }

    [Fact]
    public void LocationDto_ShouldAllowCoordinatesOnly()
    {
        // Act - Coordinates only
        var coordsOnly = new LocationDto(null, 48.8566, 2.3522);

        // Assert - Test all branches
        coordsOnly.Name.Should().BeNull();
        coordsOnly.Latitude.Should().Be(48.8566);
        coordsOnly.Longitude.Should().Be(2.3522);
    }

    [Fact]
    public void LocationDto_SingleParameterConstructor_ShouldWork()
    {
        var dto = new LocationDto("Garage");
        dto.Name.Should().Be("Garage");
        dto.Latitude.Should().BeNull();
    }
}

#endregion

#region Extended DTO Coverage Tests

public class NodeSensorAssignmentDtoCoverageTests
{
    [Fact]
    public void NodeSensorAssignmentDto_AllPinOverrides()
    {
        var dto = new NodeSensorAssignmentDto(
            Id: Guid.NewGuid(), NodeId: Guid.NewGuid(), NodeName: "Node",
            SensorId: Guid.NewGuid(), SensorCode: "BME280", SensorName: "BME280",
            EndpointId: 1, Alias: "Alias", I2CAddressOverride: "0x77",
            SdaPinOverride: 19, SclPinOverride: 18, OneWirePinOverride: 5,
            AnalogPinOverride: 35, DigitalPinOverride: 6, TriggerPinOverride: 14,
            EchoPinOverride: 15, BaudRateOverride: null, IntervalSecondsOverride: 60, IsActive: true,
            LastSeenAt: DateTime.UtcNow, AssignedAt: DateTime.UtcNow,
            EffectiveConfig: new EffectiveConfigDto(30, "0x76", 21, 22, 4, 34, 5, 12, 13, null, 0.5, 1.1)
        );
        dto.SdaPinOverride.Should().Be(19);
        dto.EchoPinOverride.Should().Be(15);
    }
}

public class SensorDtoCoverageTests
{
    [Fact]
    public void SensorDto_WithCapabilities()
    {
        var sensorId = Guid.NewGuid();
        var cap = new SensorCapabilityDto(
            Id: Guid.NewGuid(),
            SensorId: sensorId,
            MeasurementType: "temperature",
            DisplayName: "Temperature",
            Unit: "°C",
            MinValue: -40,
            MaxValue: 85,
            Resolution: 0.01,
            Accuracy: 0.5,
            MatterClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            SortOrder: 1,
            IsActive: true
        );
        var dto = new SensorDto(
            Id: sensorId,
            TenantId: Guid.NewGuid(),
            Code: "BME280",
            Name: "BME280 Sensor",
            Description: "Temperature/Humidity/Pressure Sensor",
            SerialNumber: "SN-12345",
            Manufacturer: "Bosch",
            Model: "BME280",
            DatasheetUrl: "https://example.com/bme280.pdf",
            Protocol: CommunicationProtocolDto.I2C,
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            BaudRate: null,
            IntervalSeconds: 60,
            MinIntervalSeconds: 1,
            WarmupTimeMs: 100,
            OffsetCorrection: 0.5,
            GainCorrection: 1.01,
            LastCalibratedAt: DateTime.UtcNow.AddDays(-30),
            CalibrationNotes: "Factory calibrated",
            CalibrationDueAt: DateTime.UtcNow.AddDays(335),
            Category: "environmental",
            Icon: "thermostat",
            Color: "#FF5722",
            Capabilities: new List<SensorCapabilityDto> { cap },
            IsActive: true,
            CreatedAt: DateTime.UtcNow.AddDays(-60),
            UpdatedAt: DateTime.UtcNow
        );
        dto.Capabilities.Should().HaveCount(1);
        dto.Manufacturer.Should().Be("Bosch");
        dto.I2CAddress.Should().Be("0x76");
    }
}

public class AlertDtoCoverageTests
{
    [Theory]
    [InlineData(AlertLevelDto.Ok)]
    [InlineData(AlertLevelDto.Info)]
    [InlineData(AlertLevelDto.Warning)]
    [InlineData(AlertLevelDto.Critical)]
    public void AlertDto_AllLevels(AlertLevelDto level)
    {
        var dto = new AlertDto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Hub",
            Guid.NewGuid(), "Node", Guid.NewGuid(), "test", "Test", level, "Msg", "Rec",
            AlertSourceDto.Local, DateTime.UtcNow, null, null, true);
        dto.Level.Should().Be(level);
    }
}

public class ReadingDtoCoverageTests
{
    [Fact]
    public void ReadingDto_AllProperties()
    {
        var dto = new ReadingDto(12345, Guid.NewGuid(), Guid.NewGuid(), "Node",
            Guid.NewGuid(), Guid.NewGuid(), "BME280", "BME280", "thermostat", "#FF5722",
            "temperature", "Temperature", 21.3, 21.5, "°C", DateTime.UtcNow,
            new LocationDto("Garden", 50.9375, 6.9603), true);
        dto.Id.Should().Be(12345);
        dto.IsSyncedToCloud.Should().BeTrue();
    }
}

public class HubDtoCoverageTests
{
    [Fact]
    public void HubDto_AllProperties()
    {
        var dto = new HubDto(Guid.NewGuid(), Guid.NewGuid(), "hub-01", "Hub", "Desc",
            DateTime.UtcNow, true, DateTime.UtcNow, 15);
        dto.SensorCount.Should().Be(15);
    }
}

public class TenantDtoCoverageTests
{
    [Fact]
    public void TenantDto_AllProperties()
    {
        // TenantDto: Id, Name, CloudApiKey, CreatedAt, LastSyncAt, IsActive
        var dto = new TenantDto(
            Id: Guid.NewGuid(),
            Name: "Home",
            CloudApiKey: "api-key-12345",
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            LastSyncAt: DateTime.UtcNow,
            IsActive: true
        );
        dto.IsActive.Should().BeTrue();
        dto.Name.Should().Be("Home");
        dto.CloudApiKey.Should().Be("api-key-12345");
    }
}

public class AlertTypeDtoCoverageTests
{
    [Fact]
    public void AlertTypeDto_AllProperties()
    {
        var dto = new AlertTypeDto(Guid.NewGuid(), "mold_risk", "Mold Risk", "Desc",
            AlertLevelDto.Warning, "warning", true, DateTime.UtcNow);
        dto.IsGlobal.Should().BeTrue();
    }
}

public class NodeHeartbeatResponseDtoCoverageTests
{
    [Fact]
    public void NodeHeartbeatResponseDto_AllProperties()
    {
        // NodeHeartbeatResponseDto: Success, ServerTime, NextHeartbeatSeconds
        var dto = new NodeHeartbeatResponseDto(
            Success: true,
            ServerTime: DateTime.UtcNow,
            NextHeartbeatSeconds: 60
        );
        dto.Success.Should().BeTrue();
        dto.NextHeartbeatSeconds.Should().Be(60);
        dto.ServerTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}

public class SensorAssignmentConfigDtoCoverageTests
{
    [Fact]
    public void SensorAssignmentConfigDto_AllProperties()
    {
        // SensorAssignmentConfigDto: EndpointId, SensorCode, SensorName, Icon, Color, IsActive, IntervalSeconds,
        // I2CAddress, SdaPin, SclPin, OneWirePin, AnalogPin, DigitalPin, TriggerPin, EchoPin,
        // OffsetCorrection, GainCorrection, Capabilities
        var capabilities = new List<SensorCapabilityConfigDto>
        {
            new("temperature", "Temperatur", "°C"),
            new("humidity", "Luftfeuchtigkeit", "%")
        };
        var dto = new SensorAssignmentConfigDto(
            EndpointId: 1,
            SensorCode: "bme280",
            SensorName: "BME280 Klima",
            Icon: "thermostat",
            Color: "#FF5722",
            IsActive: true,
            IntervalSeconds: 30,
            I2CAddress: "0x76",
            SdaPin: 21,
            SclPin: 22,
            OneWirePin: null,
            AnalogPin: null,
            DigitalPin: null,
            TriggerPin: null,
            EchoPin: null,
            OffsetCorrection: 0.5,
            GainCorrection: 1.1,
            Capabilities: capabilities
        );
        dto.OffsetCorrection.Should().Be(0.5);
        dto.Capabilities.Should().HaveCount(2);
    }
}

public class SensorLatestReadingDtoCoverageTests
{
    [Fact]
    public void SensorLatestReadingDto_AllProperties()
    {
        // SensorLatestReadingDto: AssignmentId, SensorId, DisplayName, FullName, Alias, SensorCode,
        // SensorModel, EndpointId, Icon, Color, IsActive, Measurements
        var measurements = new List<LatestMeasurementDto>
        {
            new(ReadingId: 123, MeasurementType: "temperature", DisplayName: "Temperatur",
                RawValue: 21.3, Value: 21.5, Unit: "°C", Timestamp: DateTime.UtcNow),
            new(ReadingId: 124, MeasurementType: "humidity", DisplayName: "Luftfeuchtigkeit",
                RawValue: 55.0, Value: 55.0, Unit: "%", Timestamp: DateTime.UtcNow)
        };
        var dto = new SensorLatestReadingDto(
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            DisplayName: "Klima Sensor",
            FullName: "GY-BME280 Breakout (I²C)",
            Alias: "Wohnzimmer",
            SensorCode: "bme280",
            SensorModel: "BME280",
            EndpointId: 1,
            Icon: "thermostat",
            Color: "#FF5722",
            IsActive: true,
            Measurements: measurements
        );
        dto.Measurements.Should().HaveCount(2);
        dto.Measurements[0].Value.Should().Be(21.5);
    }
}

public class CreateSyncedNodeDtoCoverageTests
{
    [Fact]
    public void CreateSyncedNodeDto_AllProperties()
    {
        var dto = new CreateSyncedNodeDto(Guid.NewGuid(), "dwd-01", "DWD",
            SyncedNodeSourceDto.Virtual, "API", new LocationDto("Cologne"), true);
        dto.Source.Should().Be(SyncedNodeSourceDto.Virtual);
    }
}

public class PaginatedResultDtoCoverageTests
{
    [Fact]
    public void PaginatedResultDto_AllProperties()
    {
        var items = new List<string> { "item1", "item2" };
        var dto = new PaginatedResultDto<string>(items, 10, 1, 5);
        dto.Items.Should().HaveCount(2);
        dto.TotalCount.Should().Be(10);
        dto.Page.Should().Be(1);
        dto.PageSize.Should().Be(5);
        dto.TotalPages.Should().Be(2);
    }

    [Fact]
    public void PaginatedResultDto_WithZeroItems()
    {
        var dto = new PaginatedResultDto<string>(new List<string>(), 0, 1, 10);
        dto.Items.Should().BeEmpty();
        dto.TotalCount.Should().Be(0);
        dto.TotalPages.Should().Be(0);
    }

    [Fact]
    public void PaginatedResultDto_HasNextPage()
    {
        var dto = new PaginatedResultDto<string>(new List<string> { "a" }, 100, 5, 10);
        dto.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResultDto_HasPreviousPage()
    {
        var dto = new PaginatedResultDto<string>(new List<string> { "a" }, 100, 5, 10);
        dto.HasPreviousPage.Should().BeTrue();
    }
}

public class LocationDtoExtendedCoverageTests
{
    [Fact]
    public void LocationDto_WithLatitudeOnly()
    {
        var dto = new LocationDto(null, 50.9375, null);
        dto.Name.Should().BeNull();
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().BeNull();
    }

    [Fact]
    public void LocationDto_WithLongitudeOnly()
    {
        var dto = new LocationDto(null, null, 6.9603);
        dto.Longitude.Should().Be(6.9603);
    }
}

public class NodeSensorAssignmentDtoExtendedCoverageTests
{
    [Fact]
    public void NodeSensorAssignmentDto_WithNullOverrides()
    {
        var dto = new NodeSensorAssignmentDto(
            Id: Guid.NewGuid(), NodeId: Guid.NewGuid(), NodeName: "Node",
            SensorId: Guid.NewGuid(), SensorCode: "BME280", SensorName: "BME280",
            EndpointId: 1, Alias: null, I2CAddressOverride: null,
            SdaPinOverride: null, SclPinOverride: null, OneWirePinOverride: null,
            AnalogPinOverride: null, DigitalPinOverride: null, TriggerPinOverride: null,
            EchoPinOverride: null, BaudRateOverride: null, IntervalSecondsOverride: null, IsActive: true,
            LastSeenAt: null, AssignedAt: DateTime.UtcNow,
            EffectiveConfig: new EffectiveConfigDto(30, null, null, null, null, null, null, null, null, null, 0, 1)
        );
        dto.Alias.Should().BeNull();
        dto.I2CAddressOverride.Should().BeNull();
    }
}

public class SyncedNodeDtoCoverageTests
{
    [Fact]
    public void SyncedNodeDto_AllProperties()
    {
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "synced-01",
            Name: "Synced Node",
            Source: SyncedNodeSourceDto.Virtual,
            SourceDetails: "DWD Station Cologne",
            Location: new LocationDto("Cologne", 50.9375, 6.9603),
            IsOnline: true,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );
        dto.Source.Should().Be(SyncedNodeSourceDto.Virtual);
        dto.SourceDetails.Should().Be("DWD Station Cologne");
    }

    [Theory]
    [InlineData(SyncedNodeSourceDto.Direct)]
    [InlineData(SyncedNodeSourceDto.Virtual)]
    [InlineData(SyncedNodeSourceDto.OtherHub)]
    public void SyncedNodeDto_AllSourceTypes(SyncedNodeSourceDto source)
    {
        var dto = new SyncedNodeDto(
            Id: Guid.NewGuid(),
            CloudNodeId: Guid.NewGuid(),
            NodeId: "synced-01",
            Name: "Synced Node",
            Source: source,
            SourceDetails: "Details",
            Location: null,
            IsOnline: true,
            LastSyncAt: DateTime.UtcNow,
            CreatedAt: DateTime.UtcNow
        );
        dto.Source.Should().Be(source);
    }
}

public class UnifiedNodeDtoCoverageTests
{
    [Fact]
    public void UnifiedNodeDto_LocalNode()
    {
        var readings = new List<UnifiedReadingDto>
        {
            new UnifiedReadingDto("temperature", "Temperature", 21.5, "°C", DateTime.UtcNow, UnifiedNodeSourceDto.Local)
        };
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "local-01",
            Name: "Local Node",
            Source: UnifiedNodeSourceDto.Local,
            SourceDetails: null,
            Sensors: null,
            Location: new LocationDto("Living Room"),
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            LatestReadings: readings
        );
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
        dto.LatestReadings.Should().HaveCount(1);
    }

    [Fact]
    public void UnifiedNodeDto_VirtualNode()
    {
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "dwd-cologne",
            Name: "DWD Cologne",
            Source: UnifiedNodeSourceDto.Virtual,
            SourceDetails: "Deutscher Wetterdienst",
            Sensors: null,
            Location: new LocationDto("Cologne", 50.9375, 6.9603),
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            LatestReadings: null
        );
        dto.Source.Should().Be(UnifiedNodeSourceDto.Virtual);
        dto.SourceDetails.Should().Be("Deutscher Wetterdienst");
    }

    [Theory]
    [InlineData(UnifiedNodeSourceDto.Local)]
    [InlineData(UnifiedNodeSourceDto.Direct)]
    [InlineData(UnifiedNodeSourceDto.Virtual)]
    public void UnifiedNodeDto_AllSourceTypes(UnifiedNodeSourceDto source)
    {
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "node-01",
            Name: "Test Node",
            Source: source,
            SourceDetails: null,
            Sensors: null,
            Location: null,
            IsOnline: true,
            LastSeen: null,
            LatestReadings: null
        );
        dto.Source.Should().Be(source);
    }
}

public class UnifiedReadingDtoCoverageTests
{
    [Fact]
    public void UnifiedReadingDto_AllProperties()
    {
        var dto = new UnifiedReadingDto(
            SensorTypeId: "temperature",
            SensorTypeName: "Temperature",
            Value: 21.5,
            Unit: "°C",
            Timestamp: DateTime.UtcNow,
            Source: UnifiedNodeSourceDto.Local
        );
        dto.Value.Should().Be(21.5);
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
    }
}

public class AlertDtoExtendedCoverageTests
{
    [Fact]
    public void AlertDto_WithAllNullableFields()
    {
        var dto = new AlertDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: null,
            HubName: null,
            NodeId: null,
            NodeName: null,
            AlertTypeId: Guid.NewGuid(),
            AlertTypeCode: "mold_risk",
            AlertTypeName: "Mold Risk",
            Level: AlertLevelDto.Warning,
            Message: "Test",
            Recommendation: null,
            Source: AlertSourceDto.Local,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: null,
            AcknowledgedAt: null,
            IsActive: true
        );
        dto.HubId.Should().BeNull();
        dto.NodeId.Should().BeNull();
        dto.Recommendation.Should().BeNull();
    }

    [Fact]
    public void AlertDto_WithExpiresAt()
    {
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var dto = new AlertDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            HubName: "Hub",
            NodeId: Guid.NewGuid(),
            NodeName: "Node",
            AlertTypeId: Guid.NewGuid(),
            AlertTypeCode: "mold_risk",
            AlertTypeName: "Mold Risk",
            Level: AlertLevelDto.Critical,
            Message: "Critical alert",
            Recommendation: "Take action",
            Source: AlertSourceDto.Cloud,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: expiresAt,
            AcknowledgedAt: null,
            IsActive: true
        );
        dto.ExpiresAt.Should().Be(expiresAt);
    }
}

public class NodeDtoExtendedCoverageTests
{
    [Fact]
    public void NodeDto_WithAllFields()
    {
        var dto = new NodeDto(
            Id: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            NodeId: "node-complete",
            Name: "Complete Node",
            Protocol: ProtocolDto.WLAN,
            Location: new LocationDto("Garden", 50.0, 7.0),
            AssignmentCount: 5,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: "2.1.0",
            BatteryLevel: 85,
            CreatedAt: DateTime.UtcNow,
            MacAddress: "AA:BB:CC:DD:EE:FF",
            Status: NodeProvisioningStatusDto.Configured,
            IsSimulation: false,
            StorageMode: StorageModeDto.RemoteOnly,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null,
            DebugLevel: DebugLevelDto.Normal,
            EnableRemoteLogging: false,
            LastDebugChange: null
        );
        dto.Protocol.Should().Be(ProtocolDto.WLAN);
        dto.FirmwareVersion.Should().Be("2.1.0");
        dto.BatteryLevel.Should().Be(85);
        dto.MacAddress.Should().Be("AA:BB:CC:DD:EE:FF");
    }

    [Fact]
    public void NodeDto_WithNullOptionalFields()
    {
        var dto = new NodeDto(
            Id: Guid.NewGuid(),
            HubId: Guid.NewGuid(),
            NodeId: "minimal-node",
            Name: "Minimal Node",
            Protocol: ProtocolDto.Unknown,
            Location: null,
            AssignmentCount: 0,
            LastSeen: null,
            IsOnline: false,
            FirmwareVersion: null,
            BatteryLevel: null,
            CreatedAt: DateTime.UtcNow,
            MacAddress: null!,
            Status: NodeProvisioningStatusDto.Unconfigured,
            IsSimulation: true,
            StorageMode: StorageModeDto.RemoteOnly,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null,
            DebugLevel: DebugLevelDto.Normal,
            EnableRemoteLogging: false,
            LastDebugChange: null
        );
        dto.Location.Should().BeNull();
        dto.FirmwareVersion.Should().BeNull();
        dto.BatteryLevel.Should().BeNull();
    }
}

public class SensorCapabilityDtoExtendedCoverageTests
{
    [Fact]
    public void SensorCapabilityDto_WithMatterCluster()
    {
        var dto = new SensorCapabilityDto(
            Id: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            MeasurementType: "temperature",
            DisplayName: "Temperature",
            Unit: "°C",
            MinValue: -40,
            MaxValue: 85,
            Resolution: 0.01,
            Accuracy: 0.5,
            MatterClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            SortOrder: 1,
            IsActive: true
        );
        dto.MatterClusterId.Should().Be(0x0402u);
        dto.MatterClusterName.Should().Be("TemperatureMeasurement");
    }

    [Fact]
    public void SensorCapabilityDto_WithoutMatterCluster()
    {
        var dto = new SensorCapabilityDto(
            Id: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            MeasurementType: "soil_moisture",
            DisplayName: "Soil Moisture",
            Unit: "%",
            MinValue: 0,
            MaxValue: 100,
            Resolution: 1,
            Accuracy: 5,
            MatterClusterId: null,
            MatterClusterName: null,
            SortOrder: 1,
            IsActive: true
        );
        dto.MatterClusterId.Should().BeNull();
        dto.MatterClusterName.Should().BeNull();
    }
}

public class ReadingDtoExtendedCoverageTests
{
    [Fact]
    public void ReadingDto_WithLocation()
    {
        var dto = new ReadingDto(
            Id: 1,
            TenantId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            NodeName: "Node",
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            SensorCode: "BME280",
            SensorName: "BME280 Sensor",
            SensorIcon: "thermostat",
            SensorColor: "#FF5722",
            MeasurementType: "temperature",
            DisplayName: "Temperature",
            RawValue: 21.3,
            Value: 21.5,
            Unit: "°C",
            Timestamp: DateTime.UtcNow,
            Location: new LocationDto("Garden", 50.9375, 6.9603),
            IsSyncedToCloud: true
        );
        dto.Location.Should().NotBeNull();
        dto.Location!.Name.Should().Be("Garden");
        dto.IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public void ReadingDto_WithoutLocation()
    {
        var dto = new ReadingDto(
            Id: 2,
            TenantId: Guid.NewGuid(),
            NodeId: Guid.NewGuid(),
            NodeName: "Node",
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            SensorCode: "DHT22",
            SensorName: "DHT22 Sensor",
            SensorIcon: "water_drop",
            SensorColor: "#2196F3",
            MeasurementType: "humidity",
            DisplayName: "Humidity",
            RawValue: 55.0,
            Value: 55.5,
            Unit: "%",
            Timestamp: DateTime.UtcNow,
            Location: null,
            IsSyncedToCloud: false
        );
        dto.Location.Should().BeNull();
        dto.IsSyncedToCloud.Should().BeFalse();
    }
}

public class LocationDtoMoreCoverageTests
{
    [Fact]
    public void LocationDto_WithOnlyName_HasNullCoordinates()
    {
        var dto = new LocationDto(Name: "Living Room", Latitude: null, Longitude: null);
        dto.Name.Should().Be("Living Room");
        dto.Latitude.Should().BeNull();
        dto.Longitude.Should().BeNull();
    }

    [Fact]
    public void LocationDto_WithOnlyCoordinates_HasNullName()
    {
        var dto = new LocationDto(Name: null, Latitude: 50.9375, Longitude: 6.9603);
        dto.Name.Should().BeNull();
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void LocationDto_WithAllFields_HasAllValues()
    {
        var dto = new LocationDto(Name: "Garden", Latitude: 50.9375, Longitude: 6.9603);
        dto.Name.Should().Be("Garden");
        dto.Latitude.Should().Be(50.9375);
        dto.Longitude.Should().Be(6.9603);
    }
}

public class AlertTypeDtoMoreCoverageTests
{
    [Fact]
    public void AlertTypeDto_WithAllOptionalFields()
    {
        var dto = new AlertTypeDto(
            Id: Guid.NewGuid(),
            Code: "mold_risk",
            Name: "Schimmelrisiko",
            Description: "Warning for mold risk",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning",
            IsGlobal: true,
            CreatedAt: DateTime.UtcNow
        );
        dto.Description.Should().Be("Warning for mold risk");
        dto.IconName.Should().Be("warning");
        dto.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void AlertTypeDto_WithNullDescriptionAndIcon()
    {
        var dto = new AlertTypeDto(
            Id: Guid.NewGuid(),
            Code: "custom_alert",
            Name: "Custom Alert",
            Description: null,
            DefaultLevel: AlertLevelDto.Info,
            IconName: null,
            IsGlobal: false,
            CreatedAt: DateTime.UtcNow
        );
        dto.Description.Should().BeNull();
        dto.IconName.Should().BeNull();
        dto.IsGlobal.Should().BeFalse();
    }
}

public class HubDtoMoreCoverageTests
{
    [Fact]
    public void HubDto_WithAllFields()
    {
        var dto = new HubDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: "hub-full",
            Name: "Full Hub",
            Description: "Full Hub Description",
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 10
        );
        dto.Description.Should().Be("Full Hub Description");
        dto.SensorCount.Should().Be(10);
    }

    [Fact]
    public void HubDto_WithNullOptionalFields()
    {
        var dto = new HubDto(
            Id: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            HubId: "hub-minimal",
            Name: "Minimal Hub",
            Description: null,
            LastSeen: null,
            IsOnline: false,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 0
        );
        dto.Description.Should().BeNull();
        dto.LastSeen.Should().BeNull();
    }
}

// NodeSensorAssignmentDtoMoreCoverageTests, SensorDtoMoreCoverageTests removed - DTO structures have changed

public class SensorLatestReadingDtoExtendedCoverageTests
{
    [Fact]
    public void SensorLatestReadingDto_WithAllFields()
    {
        var dto = new SensorLatestReadingDto(
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            DisplayName: "Living Room Sensor",
            FullName: "BME280 Climate Sensor",
            Alias: "LR-Sensor",
            SensorCode: "BME280",
            SensorModel: "BME280",
            EndpointId: 1,
            Icon: "thermostat",
            Color: "#FF5722",
            IsActive: true,
            Measurements: [
                new LatestMeasurementDto(1, "temperature", "Temperatur", 21.3, 21.5, "°C", DateTime.UtcNow)
            ]
        );
        dto.Alias.Should().Be("LR-Sensor");
        dto.Icon.Should().Be("thermostat");
        dto.Color.Should().Be("#FF5722");
    }

    [Fact]
    public void SensorLatestReadingDto_WithNullOptionalFields()
    {
        var dto = new SensorLatestReadingDto(
            AssignmentId: Guid.NewGuid(),
            SensorId: Guid.NewGuid(),
            DisplayName: "Sensor",
            FullName: "Basic Sensor",
            Alias: null,
            SensorCode: "BASIC",
            SensorModel: "Basic",
            EndpointId: 1,
            Icon: null,
            Color: null,
            IsActive: true,
            Measurements: []
        );
        dto.Alias.Should().BeNull();
        dto.Icon.Should().BeNull();
        dto.Color.Should().BeNull();
    }
}

public class TenantDtoExtendedCoverageTests
{
    [Fact]
    public void TenantDto_WithAllFields()
    {
        var dto = new TenantDto(
            Id: Guid.NewGuid(),
            Name: "Full Tenant",
            CloudApiKey: "cloud-api-key-123",
            CreatedAt: DateTime.UtcNow,
            LastSyncAt: DateTime.UtcNow,
            IsActive: true
        );
        dto.CloudApiKey.Should().Be("cloud-api-key-123");
        dto.LastSyncAt.Should().NotBeNull();
        dto.IsActive.Should().BeTrue();
    }

    [Fact]
    public void TenantDto_WithNullOptionalFields()
    {
        var dto = new TenantDto(
            Id: Guid.NewGuid(),
            Name: "Minimal Tenant",
            CloudApiKey: null,
            CreatedAt: DateTime.UtcNow,
            LastSyncAt: null,
            IsActive: false
        );
        dto.CloudApiKey.Should().BeNull();
        dto.LastSyncAt.Should().BeNull();
        dto.IsActive.Should().BeFalse();
    }
}

// CreateSyncedNodeDtoExtendedCoverageTests and SyncedNodeDtoExtendedCoverageTests removed - DTO structure has changed

public class UnifiedNodeDtoExtendedCoverageTests
{
    [Fact]
    public void UnifiedNodeDto_WithAllFields()
    {
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "unified-node-1",
            Name: "Unified Node Full",
            Source: UnifiedNodeSourceDto.Local,
            SourceDetails: "My Hub",
            Sensors: [],
            Location: new LocationDto("Unified Location", 50.0, 7.0),
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            LatestReadings: null
        );
        dto.Location.Should().NotBeNull();
        dto.LastSeen.Should().NotBeNull();
        dto.Source.Should().Be(UnifiedNodeSourceDto.Local);
        dto.SourceDetails.Should().Be("My Hub");
    }

    [Fact]
    public void UnifiedNodeDto_WithNullOptionalFields()
    {
        var dto = new UnifiedNodeDto(
            Id: Guid.NewGuid(),
            NodeId: "unified-node-2",
            Name: "Unified Node Minimal",
            Source: UnifiedNodeSourceDto.Local,
            SourceDetails: null,
            Sensors: null,
            Location: null,
            IsOnline: false,
            LastSeen: null,
            LatestReadings: null
        );
        dto.Location.Should().BeNull();
        dto.LastSeen.Should().BeNull();
        dto.SourceDetails.Should().BeNull();
    }
}

public class PaginatedResultDtoExtendedCoverageTests
{
    [Fact]
    public void PaginatedResultDto_CalculatesTotalPages_Correctly()
    {
        var dto = new PaginatedResultDto<string>(
            Items: ["item1", "item2"],
            TotalCount: 25,
            Page: 1,
            PageSize: 10
        );
        dto.TotalPages.Should().Be(3); // 25 / 10 = 2.5 -> 3
        dto.HasNextPage.Should().BeTrue();
        dto.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResultDto_OnLastPage_HasNextPageIsFalse()
    {
        var dto = new PaginatedResultDto<string>(
            Items: ["item1"],
            TotalCount: 25,
            Page: 3,
            PageSize: 10
        );
        dto.HasNextPage.Should().BeFalse();
        dto.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedResultDto_SinglePage_NoPreviousOrNext()
    {
        var dto = new PaginatedResultDto<string>(
            Items: ["item1", "item2"],
            TotalCount: 2,
            Page: 1,
            PageSize: 10
        );
        dto.TotalPages.Should().Be(1);
        dto.HasNextPage.Should().BeFalse();
        dto.HasPreviousPage.Should().BeFalse();
    }
}

#endregion
