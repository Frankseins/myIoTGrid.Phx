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
            Status: NodeProvisioningStatusDto.Configured
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
            MeasurementType: "temperature",
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
