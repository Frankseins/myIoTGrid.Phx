using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.Domain;

/// <summary>
/// Tests for Domain Entities (Matter-konform).
/// Hub → Raspberry Pi Gateway
/// Node → ESP32/LoRa32 Device (= Matter Node)
/// Sensor → Physical sensor chip DHT22, BME280 (= Matter Endpoint)
/// Reading → Sensor measurement (= Matter Attribute Report)
/// </summary>
public class EntityTests
{
    #region Tenant Entity Tests

    [Fact]
    public void Tenant_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var tenant = new Tenant();

        // Assert
        tenant.Id.Should().Be(Guid.Empty);
        tenant.Name.Should().BeEmpty();
        tenant.CloudApiKey.Should().BeNull();
        tenant.CreatedAt.Should().Be(default);
        tenant.LastSyncAt.Should().BeNull();
        tenant.IsActive.Should().BeTrue();
        tenant.Hubs.Should().NotBeNull();
        tenant.Hubs.Should().BeEmpty();
        tenant.Alerts.Should().NotBeNull();
        tenant.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void Tenant_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastSyncAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var tenant = new Tenant
        {
            Id = id,
            Name = "Test Tenant",
            CloudApiKey = "api-key-123",
            CreatedAt = createdAt,
            LastSyncAt = lastSyncAt,
            IsActive = false
        };

        // Assert
        tenant.Id.Should().Be(id);
        tenant.Name.Should().Be("Test Tenant");
        tenant.CloudApiKey.Should().Be("api-key-123");
        tenant.CreatedAt.Should().Be(createdAt);
        tenant.LastSyncAt.Should().Be(lastSyncAt);
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Tenant_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange
        var tenant = new Tenant();
        var hub = new HubEntity { Id = Guid.NewGuid() };
        var alert = new Alert { Id = Guid.NewGuid() };

        // Act
        tenant.Hubs.Add(hub);
        tenant.Alerts.Add(alert);

        // Assert
        tenant.Hubs.Should().HaveCount(1);
        tenant.Alerts.Should().HaveCount(1);
    }

    #endregion

    #region Hub Entity Tests

    [Fact]
    public void Hub_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var hub = new HubEntity();

        // Assert
        hub.Id.Should().Be(Guid.Empty);
        hub.TenantId.Should().Be(Guid.Empty);
        hub.HubId.Should().BeEmpty();
        hub.Name.Should().BeEmpty();
        hub.Description.Should().BeNull();
        hub.LastSeen.Should().BeNull();
        hub.IsOnline.Should().BeFalse();
        hub.CreatedAt.Should().Be(default);
        hub.Tenant.Should().BeNull();
        hub.Nodes.Should().NotBeNull();
        hub.Nodes.Should().BeEmpty();
        hub.Alerts.Should().NotBeNull();
        hub.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void Hub_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastSeen = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var hub = new HubEntity
        {
            Id = id,
            TenantId = tenantId,
            HubId = "hub-home-01",
            Name = "Home Hub",
            Description = "Main hub at home",
            LastSeen = lastSeen,
            IsOnline = true,
            CreatedAt = createdAt
        };

        // Assert
        hub.Id.Should().Be(id);
        hub.TenantId.Should().Be(tenantId);
        hub.HubId.Should().Be("hub-home-01");
        hub.Name.Should().Be("Home Hub");
        hub.Description.Should().Be("Main hub at home");
        hub.LastSeen.Should().Be(lastSeen);
        hub.IsOnline.Should().BeTrue();
        hub.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Hub_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange
        var hub = new HubEntity();
        var node = new Node { Id = Guid.NewGuid() };
        var alert = new Alert { Id = Guid.NewGuid() };

        // Act
        hub.Nodes.Add(node);
        hub.Alerts.Add(alert);

        // Assert
        hub.Nodes.Should().HaveCount(1);
        hub.Alerts.Should().HaveCount(1);
    }

    [Fact]
    public void Hub_ShouldSupportTenantNavigation()
    {
        // Arrange
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test" };
        var hub = new HubEntity { Tenant = tenant };

        // Assert
        hub.Tenant.Should().Be(tenant);
        hub.Tenant!.Name.Should().Be("Test");
    }

    #endregion

    #region Node Entity Tests (ESP32/LoRa32 = Matter Node)

    [Fact]
    public void Node_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var node = new Node();

        // Assert
        node.Id.Should().Be(Guid.Empty);
        node.HubId.Should().Be(Guid.Empty);
        node.NodeId.Should().BeEmpty();
        node.Name.Should().BeEmpty();
        node.Protocol.Should().Be(Protocol.WLAN);
        node.Location.Should().BeNull();
        node.LastSeen.Should().BeNull();
        node.IsOnline.Should().BeFalse();
        node.FirmwareVersion.Should().BeNull();
        node.BatteryLevel.Should().BeNull();
        node.CreatedAt.Should().Be(default);
        node.Hub.Should().BeNull();
        node.SensorAssignments.Should().NotBeNull();
        node.SensorAssignments.Should().BeEmpty();
        node.Readings.Should().NotBeNull();
        node.Readings.Should().BeEmpty();
    }

    [Fact]
    public void Node_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastSeen = DateTime.UtcNow.AddMinutes(-2);
        var location = new Location("Wohnzimmer", 50.0, 6.0);

        // Act
        var node = new Node
        {
            Id = id,
            HubId = hubId,
            NodeId = "node-wohnzimmer-01",
            Name = "Living Room Node",
            Protocol = Protocol.LoRaWAN,
            Location = location,
            LastSeen = lastSeen,
            IsOnline = true,
            FirmwareVersion = "2.0.1",
            BatteryLevel = 85,
            CreatedAt = createdAt
        };

        // Assert
        node.Id.Should().Be(id);
        node.HubId.Should().Be(hubId);
        node.NodeId.Should().Be("node-wohnzimmer-01");
        node.Name.Should().Be("Living Room Node");
        node.Protocol.Should().Be(Protocol.LoRaWAN);
        node.Location.Should().Be(location);
        node.LastSeen.Should().Be(lastSeen);
        node.IsOnline.Should().BeTrue();
        node.FirmwareVersion.Should().Be("2.0.1");
        node.BatteryLevel.Should().Be(85);
        node.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Node_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange - New 3-tier model: Node has SensorAssignments, not Sensors
        var node = new Node();
        var assignment = new NodeSensorAssignment { Id = Guid.NewGuid() };
        var reading = new Reading { Id = 1 };

        // Act
        node.SensorAssignments.Add(assignment);
        node.Readings.Add(reading);

        // Assert
        node.SensorAssignments.Should().HaveCount(1);
        node.Readings.Should().HaveCount(1);
    }

    [Fact]
    public void Node_BatteryLevel_ShouldAcceptZeroToHundred()
    {
        // Arrange & Act
        var nodeZero = new Node { BatteryLevel = 0 };
        var nodeFull = new Node { BatteryLevel = 100 };
        var nodeMid = new Node { BatteryLevel = 50 };

        // Assert
        nodeZero.BatteryLevel.Should().Be(0);
        nodeFull.BatteryLevel.Should().Be(100);
        nodeMid.BatteryLevel.Should().Be(50);
    }

    #endregion

    #region Sensor Entity Tests (v3.0 Two-Tier: Sensor has Code/Name directly, no SensorType)

    [Fact]
    public void Sensor_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act - v3.0 Two-Tier: Sensor has Code, Name, Protocol, Category directly
        var sensor = new Sensor();

        // Assert
        sensor.Id.Should().Be(Guid.Empty);
        sensor.TenantId.Should().Be(Guid.Empty);
        sensor.Code.Should().BeEmpty();
        sensor.Name.Should().BeEmpty();
        sensor.Protocol.Should().Be(default);
        sensor.Category.Should().BeEmpty();
        sensor.Description.Should().BeNull();
        sensor.SerialNumber.Should().BeNull();
        sensor.IntervalSeconds.Should().Be(60);
        sensor.MinIntervalSeconds.Should().Be(1); // Default is 1 in v3.0
        sensor.OffsetCorrection.Should().Be(0);
        sensor.GainCorrection.Should().Be(1.0);
        sensor.LastCalibratedAt.Should().BeNull();
        sensor.CalibrationNotes.Should().BeNull();
        sensor.CalibrationDueAt.Should().BeNull();
        sensor.IsActive.Should().BeTrue();
        sensor.CreatedAt.Should().Be(default);
        sensor.UpdatedAt.Should().Be(default);
        sensor.Tenant.Should().BeNull();
        sensor.NodeAssignments.Should().NotBeNull();
        sensor.NodeAssignments.Should().BeEmpty();
        sensor.Capabilities.Should().NotBeNull();
        sensor.Capabilities.Should().BeEmpty();
    }

    [Fact]
    public void Sensor_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var updatedAt = DateTime.UtcNow;
        var lastCalibratedAt = DateTime.UtcNow.AddMonths(-1);
        var calibrationDueAt = DateTime.UtcNow.AddMonths(5);

        // Act - v3.0 Two-Tier: Sensor has Code, Name, Protocol, Category, IntervalSeconds
        var sensor = new Sensor
        {
            Id = id,
            TenantId = tenantId,
            Code = "dht22-living-room",
            Name = "Living Room DHT22",
            Protocol = CommunicationProtocol.OneWire,
            Category = "climate",
            Description = "Temperature and humidity sensor",
            SerialNumber = "DHT22-001",
            IntervalSeconds = 30,
            MinIntervalSeconds = 2,
            OffsetCorrection = 0.5,
            GainCorrection = 1.02,
            LastCalibratedAt = lastCalibratedAt,
            CalibrationNotes = "Calibrated with reference",
            CalibrationDueAt = calibrationDueAt,
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        // Assert
        sensor.Id.Should().Be(id);
        sensor.TenantId.Should().Be(tenantId);
        sensor.Code.Should().Be("dht22-living-room");
        sensor.Name.Should().Be("Living Room DHT22");
        sensor.Protocol.Should().Be(CommunicationProtocol.OneWire);
        sensor.Category.Should().Be("climate");
        sensor.Description.Should().Be("Temperature and humidity sensor");
        sensor.SerialNumber.Should().Be("DHT22-001");
        sensor.IntervalSeconds.Should().Be(30);
        sensor.MinIntervalSeconds.Should().Be(2);
        sensor.OffsetCorrection.Should().Be(0.5);
        sensor.GainCorrection.Should().Be(1.02);
        sensor.LastCalibratedAt.Should().Be(lastCalibratedAt);
        sensor.CalibrationNotes.Should().Be("Calibrated with reference");
        sensor.CalibrationDueAt.Should().Be(calibrationDueAt);
        sensor.IsActive.Should().BeTrue();
        sensor.CreatedAt.Should().Be(createdAt);
        sensor.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void Sensor_Calibration_ShouldAllowDifferentValues()
    {
        // Arrange & Act - v3.0 Two-Tier: Test calibration settings
        var sensor1 = new Sensor { OffsetCorrection = -0.5, GainCorrection = 0.98 };
        var sensor2 = new Sensor { OffsetCorrection = 0, GainCorrection = 1.0 };
        var sensor3 = new Sensor { OffsetCorrection = 2.0, GainCorrection = 1.05 };

        // Assert
        sensor1.OffsetCorrection.Should().Be(-0.5);
        sensor1.GainCorrection.Should().Be(0.98);
        sensor2.OffsetCorrection.Should().Be(0);
        sensor2.GainCorrection.Should().Be(1.0);
        sensor3.OffsetCorrection.Should().Be(2.0);
        sensor3.GainCorrection.Should().Be(1.05);
    }

    #endregion

    #region Reading Entity Tests (= Matter Attribute Report)

    [Fact]
    public void Reading_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act - New model: Reading has AssignmentId, MeasurementType, RawValue, Value, Unit
        var reading = new Reading();

        // Assert
        reading.Id.Should().Be(0);  // long type, default is 0
        reading.TenantId.Should().Be(Guid.Empty);
        reading.NodeId.Should().Be(Guid.Empty);
        reading.AssignmentId.Should().BeNull();  // AssignmentId is nullable in v3.0
        reading.MeasurementType.Should().BeEmpty();
        reading.RawValue.Should().Be(0);
        reading.Value.Should().Be(0);
        reading.Unit.Should().BeEmpty();
        reading.Timestamp.Should().Be(default);
        reading.IsSyncedToCloud.Should().BeFalse();
        reading.Node.Should().BeNull();
        reading.Assignment.Should().BeNull();
    }

    [Fact]
    public void Reading_ShouldSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act - New model: Reading stores both RawValue and calibrated Value
        var reading = new Reading
        {
            Id = 12345,
            TenantId = tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            RawValue = 23.3,
            Value = 23.5,  // Calibrated value
            Unit = "°C",
            Timestamp = timestamp,
            IsSyncedToCloud = true
        };

        // Assert
        reading.Id.Should().Be(12345);
        reading.TenantId.Should().Be(tenantId);
        reading.NodeId.Should().Be(nodeId);
        reading.AssignmentId.Should().Be(assignmentId);
        reading.MeasurementType.Should().Be("temperature");
        reading.RawValue.Should().Be(23.3);
        reading.Value.Should().Be(23.5);
        reading.Unit.Should().Be("°C");
        reading.Timestamp.Should().Be(timestamp);
        reading.IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public void Reading_Values_ShouldAcceptNegativeValues()
    {
        // Arrange & Act - New model: Both RawValue and Value can be negative
        var reading = new Reading { RawValue = -40.0, Value = -39.5 };

        // Assert
        reading.RawValue.Should().Be(-40.0);
        reading.Value.Should().Be(-39.5);
    }

    [Fact]
    public void Reading_Values_ShouldAcceptDecimalValues()
    {
        // Arrange & Act - New model: Test precision
        var reading = new Reading { RawValue = 21.123456789, Value = 21.654321 };

        // Assert
        reading.RawValue.Should().BeApproximately(21.123456789, 0.0000001);
        reading.Value.Should().BeApproximately(21.654321, 0.0000001);
    }

    [Fact]
    public void Reading_Id_ShouldBeLongForPerformance()
    {
        // Arrange & Act
        var reading = new Reading { Id = long.MaxValue };

        // Assert
        reading.Id.Should().Be(long.MaxValue);
    }

    #endregion

    #region SensorCapability Entity Tests (v3.0 Two-Tier: belongs to Sensor, not SensorType)

    [Fact]
    public void SensorCapability_ContainsMatterClusterInfo()
    {
        // Arrange & Act - v3.0 Two-Tier: SensorCapability belongs to Sensor
        var capability = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = Guid.NewGuid(),
            MeasurementType = "temperature",
            DisplayName = "Temperature",
            Unit = "°C",
            MatterClusterId = 0x0402,
            MatterClusterName = "TemperatureMeasurement",
            MinValue = -40,
            MaxValue = 80,
            IsActive = true
        };

        // Assert
        capability.MatterClusterId.Should().Be(0x0402);
        capability.MatterClusterName.Should().Be("TemperatureMeasurement");
        capability.MeasurementType.Should().Be("temperature");
        capability.DisplayName.Should().Be("Temperature");
        capability.Unit.Should().Be("°C");
        capability.MinValue.Should().Be(-40);
        capability.MaxValue.Should().Be(80);
    }

    [Fact]
    public void Sensor_Capabilities_ShouldAllowAddingItems()
    {
        // Arrange - v3.0 Two-Tier: Sensor has Capabilities directly
        var sensor = new Sensor { Id = Guid.NewGuid(), Code = "dht22" };
        var capability = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            MeasurementType = "temperature",
            Unit = "°C"
        };

        // Act
        sensor.Capabilities.Add(capability);

        // Assert
        sensor.Capabilities.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(CommunicationProtocol.I2C)]
    [InlineData(CommunicationProtocol.SPI)]
    [InlineData(CommunicationProtocol.OneWire)]
    [InlineData(CommunicationProtocol.Analog)]
    [InlineData(CommunicationProtocol.UART)]
    [InlineData(CommunicationProtocol.Digital)]
    [InlineData(CommunicationProtocol.UltraSonic)]
    public void Sensor_Protocol_SupportsDifferentProtocols(CommunicationProtocol protocol)
    {
        // Arrange & Act - v3.0 Two-Tier: Sensor has Protocol directly
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Code = "test",
            Protocol = protocol
        };

        // Assert
        sensor.Protocol.Should().Be(protocol);
    }

    #endregion

    #region Alert Entity Tests (with NodeId instead of SensorId)

    [Fact]
    public void Alert_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var alert = new Alert();

        // Assert
        alert.Id.Should().Be(Guid.Empty);
        alert.TenantId.Should().Be(Guid.Empty);
        alert.HubId.Should().BeNull();
        alert.NodeId.Should().BeNull();
        alert.AlertTypeId.Should().Be(Guid.Empty);
        alert.Level.Should().Be(AlertLevel.Ok);
        alert.Message.Should().BeEmpty();
        alert.Recommendation.Should().BeNull();
        alert.Source.Should().Be(AlertSource.Local);
        alert.CreatedAt.Should().Be(default);
        alert.ExpiresAt.Should().BeNull();
        alert.AcknowledgedAt.Should().BeNull();
        alert.IsActive.Should().BeTrue();
        alert.Tenant.Should().BeNull();
        alert.Hub.Should().BeNull();
        alert.Node.Should().BeNull();
        alert.AlertType.Should().BeNull();
    }

    [Fact]
    public void Alert_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var alertTypeId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var expiresAt = DateTime.UtcNow.AddHours(24);
        var acknowledgedAt = DateTime.UtcNow.AddHours(1);

        // Act
        var alert = new Alert
        {
            Id = id,
            TenantId = tenantId,
            HubId = hubId,
            NodeId = nodeId,
            AlertTypeId = alertTypeId,
            Level = AlertLevel.Critical,
            Message = "Critical alert!",
            Recommendation = "Take action immediately",
            Source = AlertSource.Cloud,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            AcknowledgedAt = acknowledgedAt,
            IsActive = false
        };

        // Assert
        alert.Id.Should().Be(id);
        alert.TenantId.Should().Be(tenantId);
        alert.HubId.Should().Be(hubId);
        alert.NodeId.Should().Be(nodeId);
        alert.AlertTypeId.Should().Be(alertTypeId);
        alert.Level.Should().Be(AlertLevel.Critical);
        alert.Message.Should().Be("Critical alert!");
        alert.Recommendation.Should().Be("Take action immediately");
        alert.Source.Should().Be(AlertSource.Cloud);
        alert.CreatedAt.Should().Be(createdAt);
        alert.ExpiresAt.Should().Be(expiresAt);
        alert.AcknowledgedAt.Should().Be(acknowledgedAt);
        alert.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(AlertLevel.Ok)]
    [InlineData(AlertLevel.Info)]
    [InlineData(AlertLevel.Warning)]
    [InlineData(AlertLevel.Critical)]
    public void Alert_Level_ShouldAcceptAllLevels(AlertLevel level)
    {
        // Arrange & Act
        var alert = new Alert { Level = level };

        // Assert
        alert.Level.Should().Be(level);
    }

    [Theory]
    [InlineData(AlertSource.Local)]
    [InlineData(AlertSource.Cloud)]
    public void Alert_Source_ShouldAcceptAllSources(AlertSource source)
    {
        // Arrange & Act
        var alert = new Alert { Source = source };

        // Assert
        alert.Source.Should().Be(source);
    }

    #endregion

    #region AlertType Entity Tests

    [Fact]
    public void AlertType_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var alertType = new AlertType();

        // Assert
        alertType.Id.Should().Be(Guid.Empty);
        alertType.Code.Should().BeEmpty();
        alertType.Name.Should().BeEmpty();
        alertType.Description.Should().BeNull();
        alertType.DefaultLevel.Should().Be(AlertLevel.Ok);
        alertType.IconName.Should().BeNull();
        alertType.IsGlobal.Should().BeFalse();
        alertType.CreatedAt.Should().Be(default);
        alertType.Alerts.Should().NotBeNull();
        alertType.Alerts.Should().BeEmpty();
    }

    [Fact]
    public void AlertType_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var alertType = new AlertType
        {
            Id = id,
            Code = "mold_risk",
            Name = "Schimmelrisiko",
            Description = "Warnung bei Schimmelgefahr",
            DefaultLevel = AlertLevel.Warning,
            IconName = "warning",
            IsGlobal = true,
            CreatedAt = createdAt
        };

        // Assert
        alertType.Id.Should().Be(id);
        alertType.Code.Should().Be("mold_risk");
        alertType.Name.Should().Be("Schimmelrisiko");
        alertType.Description.Should().Be("Warnung bei Schimmelgefahr");
        alertType.DefaultLevel.Should().Be(AlertLevel.Warning);
        alertType.IconName.Should().Be("warning");
        alertType.IsGlobal.Should().BeTrue();
        alertType.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void AlertType_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange
        var alertType = new AlertType();
        var alert = new Alert { Id = Guid.NewGuid() };

        // Act
        alertType.Alerts.Add(alert);

        // Assert
        alertType.Alerts.Should().HaveCount(1);
    }

    #endregion

    #region Enum Tests

    [Fact]
    public void Protocol_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)Protocol.Unknown).Should().Be(0);
        ((int)Protocol.WLAN).Should().Be(1);
        ((int)Protocol.LoRaWAN).Should().Be(2);
    }

    [Fact]
    public void AlertLevel_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)AlertLevel.Ok).Should().Be(0);
        ((int)AlertLevel.Info).Should().Be(1);
        ((int)AlertLevel.Warning).Should().Be(2);
        ((int)AlertLevel.Critical).Should().Be(3);
    }

    [Fact]
    public void AlertSource_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)AlertSource.Local).Should().Be(0);
        ((int)AlertSource.Cloud).Should().Be(1);
    }

    #endregion

    #region Entity Relationship Tests

    [Fact]
    public void Tenant_Hub_Relationship_ShouldWork()
    {
        // Arrange
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Tenant" };
        var hub = new HubEntity { Id = Guid.NewGuid(), TenantId = tenant.Id, Tenant = tenant };

        // Act
        tenant.Hubs.Add(hub);

        // Assert
        tenant.Hubs.Should().Contain(hub);
        hub.Tenant.Should().Be(tenant);
    }

    [Fact]
    public void Hub_Node_Relationship_ShouldWork()
    {
        // Arrange
        var hub = new HubEntity { Id = Guid.NewGuid(), Name = "Test Hub" };
        var node = new Node { Id = Guid.NewGuid(), HubId = hub.Id, Hub = hub };

        // Act
        hub.Nodes.Add(node);

        // Assert
        hub.Nodes.Should().Contain(node);
        node.Hub.Should().Be(hub);
    }

    [Fact]
    public void Node_SensorAssignment_Relationship_ShouldWork()
    {
        // Arrange - v3.0 Two-Tier: Node has SensorAssignments, Sensor has Code/Name directly
        var node = new Node { Id = Guid.NewGuid(), NodeId = "test-node" };
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Code = "dht22-01",
            Name = "DHT22 Sensor",
            Protocol = CommunicationProtocol.OneWire,
            Category = "climate"
        };
        var assignment = new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = node.Id,
            Node = node,
            SensorId = sensor.Id,
            Sensor = sensor,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

        // Act
        node.SensorAssignments.Add(assignment);

        // Assert
        node.SensorAssignments.Should().Contain(assignment);
        assignment.Node.Should().Be(node);
        assignment.Sensor.Should().Be(sensor);
    }

    [Fact]
    public void Node_Reading_Relationship_ShouldWork()
    {
        // Arrange
        var node = new Node { Id = Guid.NewGuid() };
        var reading = new Reading { Id = 1, NodeId = node.Id, Node = node };

        // Act
        node.Readings.Add(reading);

        // Assert
        node.Readings.Should().Contain(reading);
        reading.Node.Should().Be(node);
    }

    [Fact]
    public void Sensor_Capability_Relationship_ShouldWork()
    {
        // Arrange - v3.0 Two-Tier: Sensor has Capabilities directly (no SensorType)
        var sensorId = Guid.NewGuid();
        var sensor = new Sensor { Id = sensorId, Code = "dht22", Name = "DHT22" };
        var capability = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensorId,
            Sensor = sensor,
            MeasurementType = "temperature",
            Unit = "°C"
        };

        // Act
        sensor.Capabilities.Add(capability);

        // Assert
        sensor.Capabilities.Should().Contain(capability);
        capability.Sensor.Should().Be(sensor);
    }

    [Fact]
    public void Reading_Assignment_Relationship_ShouldWork()
    {
        // Arrange - New 3-tier model: Reading links to NodeSensorAssignment, not SensorType
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        var assignment = new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = nodeId,
            SensorId = sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };

        var reading = new Reading
        {
            TenantId = Guid.NewGuid(),
            NodeId = nodeId,
            AssignmentId = assignmentId,
            Assignment = assignment,
            MeasurementType = "temperature",
            RawValue = 21.5,
            Value = 21.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        };

        // Act
        assignment.Readings.Add(reading);

        // Assert
        assignment.Readings.Should().Contain(reading);
        reading.Assignment.Should().Be(assignment);
        reading.AssignmentId.Should().Be(assignmentId);
    }

    [Fact]
    public void AlertType_Alert_Relationship_ShouldWork()
    {
        // Arrange
        var alertType = new AlertType { Id = Guid.NewGuid(), Code = "mold_risk" };
        var alert = new Alert { Id = Guid.NewGuid(), AlertTypeId = alertType.Id, AlertType = alertType };

        // Act
        alertType.Alerts.Add(alert);

        // Assert
        alertType.Alerts.Should().Contain(alert);
        alert.AlertType.Should().Be(alertType);
    }

    [Fact]
    public void Alert_Node_Relationship_ShouldWork()
    {
        // Arrange
        var node = new Node { Id = Guid.NewGuid(), NodeId = "test-node" };
        var alert = new Alert { Id = Guid.NewGuid(), NodeId = node.Id, Node = node };

        // Assert
        alert.Node.Should().Be(node);
        alert.NodeId.Should().Be(node.Id);
    }

    #endregion

    #region Location Value Object Tests

    [Fact]
    public void Location_ShouldSetAllProperties()
    {
        // Arrange & Act
        var location = new Location("Wohnzimmer", 50.9375, 6.9603);

        // Assert
        location.Name.Should().Be("Wohnzimmer");
        location.Latitude.Should().Be(50.9375);
        location.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public void Location_ShouldAllowNullCoordinates()
    {
        // Arrange & Act
        var location = new Location("Wohnzimmer", null, null);

        // Assert
        location.Name.Should().Be("Wohnzimmer");
        location.Latitude.Should().BeNull();
        location.Longitude.Should().BeNull();
    }

    [Fact]
    public void Location_ShouldAllowNullName()
    {
        // Arrange & Act
        var location = new Location(null, 50.9375, 6.9603);

        // Assert
        location.Name.Should().BeNull();
        location.Latitude.Should().Be(50.9375);
        location.Longitude.Should().Be(6.9603);
    }

    #endregion

    #region SyncedNode Entity Tests

    [Fact]
    public void SyncedNode_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var syncedNode = new SyncedNode();

        // Assert
        syncedNode.Id.Should().Be(Guid.Empty);
        syncedNode.CloudNodeId.Should().Be(Guid.Empty);
        syncedNode.NodeId.Should().BeEmpty();
        syncedNode.Name.Should().BeEmpty();
        syncedNode.Source.Should().Be(SyncedNodeSource.Direct);
        syncedNode.SourceDetails.Should().BeNull();
        syncedNode.Location.Should().BeNull();
        syncedNode.IsOnline.Should().BeFalse();
        syncedNode.LastSyncAt.Should().Be(default);
        syncedNode.CreatedAt.Should().Be(default);
        syncedNode.SyncedReadings.Should().NotBeNull();
        syncedNode.SyncedReadings.Should().BeEmpty();
    }

    [Fact]
    public void SyncedNode_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cloudNodeId = Guid.NewGuid();
        var lastSyncAt = DateTime.UtcNow;
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var location = new Location("Köln", 50.9375, 6.9603);

        // Act
        var syncedNode = new SyncedNode
        {
            Id = id,
            CloudNodeId = cloudNodeId,
            NodeId = "dwd-cologne-01",
            Name = "DWD Köln Station",
            Source = SyncedNodeSource.Virtual,
            SourceDetails = "DWD Station: 10513",
            Location = location,
            IsOnline = true,
            LastSyncAt = lastSyncAt,
            CreatedAt = createdAt
        };

        // Assert
        syncedNode.Id.Should().Be(id);
        syncedNode.CloudNodeId.Should().Be(cloudNodeId);
        syncedNode.NodeId.Should().Be("dwd-cologne-01");
        syncedNode.Name.Should().Be("DWD Köln Station");
        syncedNode.Source.Should().Be(SyncedNodeSource.Virtual);
        syncedNode.SourceDetails.Should().Be("DWD Station: 10513");
        syncedNode.Location.Should().Be(location);
        syncedNode.IsOnline.Should().BeTrue();
        syncedNode.LastSyncAt.Should().Be(lastSyncAt);
        syncedNode.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SyncedNode_NavigationProperties_ShouldAllowAddingReadings()
    {
        // Arrange
        var syncedNode = new SyncedNode
        {
            Id = Guid.NewGuid(),
            NodeId = "test-synced-node"
        };
        var reading = new SyncedReading
        {
            Id = 1,
            SyncedNodeId = syncedNode.Id,
            SensorCode = "dht22",
            MeasurementType = "temperature",
            Value = 21.5
        };

        // Act
        syncedNode.SyncedReadings.Add(reading);

        // Assert
        syncedNode.SyncedReadings.Should().HaveCount(1);
        syncedNode.SyncedReadings.First().Should().Be(reading);
    }

    [Theory]
    [InlineData(SyncedNodeSource.Direct)]
    [InlineData(SyncedNodeSource.Virtual)]
    [InlineData(SyncedNodeSource.OtherHub)]
    public void SyncedNode_Source_ShouldAcceptAllValidValues(SyncedNodeSource source)
    {
        // Arrange & Act
        var syncedNode = new SyncedNode { Source = source };

        // Assert
        syncedNode.Source.Should().Be(source);
    }

    [Fact]
    public void SyncedNode_WithOtherHubSource_ShouldHaveSourceDetails()
    {
        // Arrange & Act
        var syncedNode = new SyncedNode
        {
            NodeId = "hub-office-node-01",
            Name = "Office Temperature Sensor",
            Source = SyncedNodeSource.OtherHub,
            SourceDetails = "Hub: Office Gateway"
        };

        // Assert
        syncedNode.Source.Should().Be(SyncedNodeSource.OtherHub);
        syncedNode.SourceDetails.Should().Be("Hub: Office Gateway");
    }

    #endregion

    #region SyncedReading Entity Tests

    [Fact]
    public void SyncedReading_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var syncedReading = new SyncedReading();

        // Assert
        syncedReading.Id.Should().Be(0);
        syncedReading.SyncedNodeId.Should().Be(Guid.Empty);
        syncedReading.SensorCode.Should().BeEmpty();
        syncedReading.MeasurementType.Should().BeEmpty();
        syncedReading.Value.Should().Be(0);
        syncedReading.Unit.Should().BeEmpty();
        syncedReading.Timestamp.Should().Be(default);
        syncedReading.SyncedAt.Should().Be(default);
        syncedReading.SyncedNode.Should().BeNull();
    }

    [Fact]
    public void SyncedReading_ShouldSetAllProperties()
    {
        // Arrange
        var syncedNodeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow.AddMinutes(-5);
        var syncedAt = DateTime.UtcNow;

        // Act
        var syncedReading = new SyncedReading
        {
            Id = 12345,
            SyncedNodeId = syncedNodeId,
            SensorCode = "bme280",
            MeasurementType = "humidity",
            Value = 65.3,
            Unit = "%",
            Timestamp = timestamp,
            SyncedAt = syncedAt
        };

        // Assert
        syncedReading.Id.Should().Be(12345);
        syncedReading.SyncedNodeId.Should().Be(syncedNodeId);
        syncedReading.SensorCode.Should().Be("bme280");
        syncedReading.MeasurementType.Should().Be("humidity");
        syncedReading.Value.Should().Be(65.3);
        syncedReading.Unit.Should().Be("%");
        syncedReading.Timestamp.Should().Be(timestamp);
        syncedReading.SyncedAt.Should().Be(syncedAt);
    }

    [Fact]
    public void SyncedReading_NavigationProperty_ShouldWork()
    {
        // Arrange
        var syncedNode = new SyncedNode
        {
            Id = Guid.NewGuid(),
            NodeId = "test-synced-node",
            Name = "Test Synced Node"
        };

        // Act
        var syncedReading = new SyncedReading
        {
            Id = 1,
            SyncedNodeId = syncedNode.Id,
            SyncedNode = syncedNode,
            SensorCode = "dht22",
            MeasurementType = "temperature",
            Value = 22.5
        };

        // Assert
        syncedReading.SyncedNode.Should().NotBeNull();
        syncedReading.SyncedNode.Should().Be(syncedNode);
        syncedReading.SyncedNodeId.Should().Be(syncedNode.Id);
    }

    [Fact]
    public void SyncedReading_ShouldSupportMultipleMeasurementTypes()
    {
        // Arrange & Act
        var syncedNodeId = Guid.NewGuid();
        var readings = new List<SyncedReading>
        {
            new SyncedReading
            {
                Id = 1,
                SyncedNodeId = syncedNodeId,
                SensorCode = "bme280",
                MeasurementType = "temperature",
                Value = 21.5,
                Unit = "°C"
            },
            new SyncedReading
            {
                Id = 2,
                SyncedNodeId = syncedNodeId,
                SensorCode = "bme280",
                MeasurementType = "humidity",
                Value = 65.0,
                Unit = "%"
            },
            new SyncedReading
            {
                Id = 3,
                SyncedNodeId = syncedNodeId,
                SensorCode = "bme280",
                MeasurementType = "pressure",
                Value = 1013.25,
                Unit = "hPa"
            }
        };

        // Assert
        readings.Should().HaveCount(3);
        readings.Select(r => r.MeasurementType).Should().Contain(new[] { "temperature", "humidity", "pressure" });
    }

    [Fact]
    public void SyncedReading_ShouldHaveLongIdForTimeSeries()
    {
        // Arrange & Act - Testing that Id is long type for high-performance time-series storage
        var reading = new SyncedReading { Id = long.MaxValue };

        // Assert
        reading.Id.Should().Be(long.MaxValue);
    }

    [Fact]
    public void SyncedReading_ShouldTrackSyncDelay()
    {
        // Arrange
        var originalTimestamp = DateTime.UtcNow.AddMinutes(-10);
        var syncedAt = DateTime.UtcNow;

        // Act
        var reading = new SyncedReading
        {
            Timestamp = originalTimestamp,
            SyncedAt = syncedAt
        };

        // Assert - Sync delay should be calculable
        var syncDelay = reading.SyncedAt - reading.Timestamp;
        syncDelay.Should().BeCloseTo(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(1));
    }

    #endregion
}
