using FluentAssertions;

namespace myIoTGrid.Hub.Service.Tests.Extensions;

public class MappingExtensionsTests
{
    #region EnumMappingExtensions Tests

    [Theory]
    [InlineData(Protocol.Unknown, ProtocolDto.Unknown)]
    [InlineData(Protocol.WLAN, ProtocolDto.WLAN)]
    [InlineData(Protocol.LoRaWAN, ProtocolDto.LoRaWAN)]
    public void Protocol_ToDto_MapsCorrectly(Protocol input, ProtocolDto expected)
    {
        var result = input.ToDto();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(ProtocolDto.Unknown, Protocol.Unknown)]
    [InlineData(ProtocolDto.WLAN, Protocol.WLAN)]
    [InlineData(ProtocolDto.LoRaWAN, Protocol.LoRaWAN)]
    public void Protocol_ToEntity_MapsCorrectly(ProtocolDto input, Protocol expected)
    {
        var result = input.ToEntity();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(AlertLevel.Ok, AlertLevelDto.Ok)]
    [InlineData(AlertLevel.Info, AlertLevelDto.Info)]
    [InlineData(AlertLevel.Warning, AlertLevelDto.Warning)]
    [InlineData(AlertLevel.Critical, AlertLevelDto.Critical)]
    public void AlertLevel_ToDto_MapsCorrectly(AlertLevel input, AlertLevelDto expected)
    {
        var result = input.ToDto();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(AlertLevelDto.Ok, AlertLevel.Ok)]
    [InlineData(AlertLevelDto.Info, AlertLevel.Info)]
    [InlineData(AlertLevelDto.Warning, AlertLevel.Warning)]
    [InlineData(AlertLevelDto.Critical, AlertLevel.Critical)]
    public void AlertLevel_ToEntity_MapsCorrectly(AlertLevelDto input, AlertLevel expected)
    {
        var result = input.ToEntity();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(AlertSource.Local, AlertSourceDto.Local)]
    [InlineData(AlertSource.Cloud, AlertSourceDto.Cloud)]
    public void AlertSource_ToDto_MapsCorrectly(AlertSource input, AlertSourceDto expected)
    {
        var result = input.ToDto();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(AlertSourceDto.Local, AlertSource.Local)]
    [InlineData(AlertSourceDto.Cloud, AlertSource.Cloud)]
    public void AlertSource_ToEntity_MapsCorrectly(AlertSourceDto input, AlertSource expected)
    {
        var result = input.ToEntity();
        result.Should().Be(expected);
    }

    #endregion

    #region LocationMappingExtensions Tests

    [Fact]
    public void Location_ToDto_WithValidLocation_ReturnsDto()
    {
        // Arrange
        var location = new Location("Wohnzimmer", 50.123, 8.456);

        // Act
        var result = location.ToDto();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Wohnzimmer");
        result.Latitude.Should().Be(50.123);
        result.Longitude.Should().Be(8.456);
    }

    [Fact]
    public void Location_ToDto_WithNullLocation_ReturnsNull()
    {
        // Arrange
        Location? location = null;

        // Act
        var result = location.ToDto();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void LocationDto_ToEntity_WithValidDto_ReturnsEntity()
    {
        // Arrange
        var dto = new LocationDto("Küche", 51.234, 9.567);

        // Act
        var result = dto.ToEntity();

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Küche");
        result.Latitude.Should().Be(51.234);
        result.Longitude.Should().Be(9.567);
    }

    [Fact]
    public void LocationDto_ToEntity_WithNullDto_ReturnsNull()
    {
        // Arrange
        LocationDto? dto = null;

        // Act
        var result = dto.ToEntity();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region HubMappingExtensions Tests

    [Fact]
    public void Hub_ToDto_MapsAllProperties()
    {
        // Arrange
        var hub = new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            HubId = "test-hub",
            Name = "Test Hub",
            Description = "A test hub",
            LastSeen = DateTime.UtcNow,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Nodes = new List<Node> { new(), new() }  // Hub has Nodes, not Sensors
        };

        // Act
        var result = hub.ToDto();

        // Assert
        result.Id.Should().Be(hub.Id);
        result.TenantId.Should().Be(hub.TenantId);
        result.HubId.Should().Be("test-hub");
        result.Name.Should().Be("Test Hub");
        result.Description.Should().Be("A test hub");
        result.LastSeen.Should().BeCloseTo(hub.LastSeen!.Value, TimeSpan.FromSeconds(1));
        result.IsOnline.Should().BeTrue();
        result.SensorCount.Should().Be(2);  // SensorCount is actually NodeCount
    }

    [Fact]
    public void Hub_ToDto_WithNodeCount_MapsCorrectly()
    {
        // Arrange
        var hub = new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            HubId = "hub-1",
            Name = "Hub 1",
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = hub.ToDto(5);

        // Assert
        result.SensorCount.Should().Be(5);
    }

    [Fact]
    public void CreateHubDto_ToEntity_MapsAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var dto = new CreateHubDto(
            HubId: "new-hub",
            Name: "New Hub",
            Description: "A new hub"
        );

        // Act
        var result = dto.ToEntity(tenantId);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.TenantId.Should().Be(tenantId);
        result.HubId.Should().Be("new-hub");
        result.Name.Should().Be("New Hub");
        result.Description.Should().Be("A new hub");
        result.IsOnline.Should().BeTrue();
        result.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreateHubDto_ToEntity_WithNullName_GeneratesNameFromHubId()
    {
        // Arrange
        var dto = new CreateHubDto(HubId: "hub-living-room");

        // Act
        var result = dto.ToEntity(Guid.NewGuid());

        // Assert
        result.Name.Should().Be("Hub Living Room");
    }

    [Theory]
    [InlineData("hub-home-01", "Hub Home 01")]
    [InlineData("sensor_wohnzimmer_temp", "Sensor Wohnzimmer Temp")]
    [InlineData("simple", "Simple")]
    public void CreateHubDto_ToEntity_GeneratesCorrectNameFromHubId(string hubId, string expectedName)
    {
        // Arrange
        var dto = new CreateHubDto(HubId: hubId);

        // Act
        var result = dto.ToEntity(Guid.NewGuid());

        // Assert
        result.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Hub_ApplyUpdate_UpdatesName()
    {
        // Arrange
        var hub = new HubEntity { Name = "Old Name" };
        var dto = new UpdateHubDto(Name: "New Name");

        // Act
        hub.ApplyUpdate(dto);

        // Assert
        hub.Name.Should().Be("New Name");
    }

    [Fact]
    public void Hub_ApplyUpdate_UpdatesDescription()
    {
        // Arrange
        var hub = new HubEntity { Description = "Old description" };
        var dto = new UpdateHubDto(Description: "New description");

        // Act
        hub.ApplyUpdate(dto);

        // Assert
        hub.Description.Should().Be("New description");
    }

    [Fact]
    public void Hub_ApplyUpdate_DoesNotUpdateNullValues()
    {
        // Arrange
        var hub = new HubEntity
        {
            Name = "Original Name",
            Description = "Original Description"
        };
        var dto = new UpdateHubDto();

        // Act
        hub.ApplyUpdate(dto);

        // Assert
        hub.Name.Should().Be("Original Name");
        hub.Description.Should().Be("Original Description");
    }

    [Fact]
    public void Hub_ToDtos_ConvertsCollection()
    {
        // Arrange
        var hubs = new List<HubEntity>
        {
            new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), HubId = "hub-1", Name = "Hub 1", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), HubId = "hub-2", Name = "Hub 2", CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = hubs.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].HubId.Should().Be("hub-1");
        result[1].HubId.Should().Be("hub-2");
    }

    #endregion

    #region NodeMappingExtensions Tests

    [Fact]
    public void Node_ToDto_MapsAllProperties()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            HubId = Guid.NewGuid(),
            NodeId = "node-1",
            Name = "Weather Station",
            Protocol = Protocol.WLAN,
            Location = new Location("Garden", null, null),
            LastSeen = DateTime.UtcNow,
            IsOnline = true,
            FirmwareVersion = "1.2.3",
            BatteryLevel = 85,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            SensorAssignments = new List<NodeSensorAssignment>()
        };

        // Act
        var result = node.ToDto();

        // Assert
        result.Id.Should().Be(node.Id);
        result.HubId.Should().Be(node.HubId);
        result.NodeId.Should().Be("node-1");
        result.Name.Should().Be("Weather Station");
        result.Protocol.Should().Be(ProtocolDto.WLAN);
        result.Location.Should().NotBeNull();
        result.Location!.Name.Should().Be("Garden");
        result.IsOnline.Should().BeTrue();
        result.FirmwareVersion.Should().Be("1.2.3");
        result.BatteryLevel.Should().Be(85);
    }

    [Fact]
    public void CreateNodeDto_ToEntity_MapsAllProperties()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        var dto = new CreateNodeDto(
            NodeId: "new-node",
            Name: "New Node",
            Protocol: ProtocolDto.LoRaWAN,
            Location: new LocationDto("Garage", 50.0, 8.0)
        );

        // Act
        var result = dto.ToEntity(hubId);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.HubId.Should().Be(hubId);
        result.NodeId.Should().Be("new-node");
        result.Name.Should().Be("New Node");
        result.Protocol.Should().Be(Protocol.LoRaWAN);
        result.Location.Should().NotBeNull();
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void CreateNodeDto_ToEntity_WithNullName_GeneratesName()
    {
        // Arrange
        var dto = new CreateNodeDto(NodeId: "node-wohnzimmer-01");

        // Act
        var result = dto.ToEntity(Guid.NewGuid());

        // Assert
        result.Name.Should().Be("Node Wohnzimmer 01");
    }

    [Fact]
    public void Node_ApplyUpdate_UpdatesAllProvidedProperties()
    {
        // Arrange
        var node = new Node
        {
            Name = "Old Name",
            Location = new Location("Old Location", null, null),
            FirmwareVersion = "1.0.0"
        };

        var dto = new UpdateNodeDto(
            Name: "New Name",
            Location: new LocationDto("New Location", 51.0, 9.0),
            FirmwareVersion: "2.0.0"
        );

        // Act
        node.ApplyUpdate(dto);

        // Assert
        node.Name.Should().Be("New Name");
        node.Location!.Name.Should().Be("New Location");
        node.FirmwareVersion.Should().Be("2.0.0");
    }

    [Fact]
    public void Node_ApplyStatus_UpdatesStatusProperties()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsOnline = false,
            BatteryLevel = 50,
            LastSeen = DateTime.UtcNow.AddHours(-1)
        };

        var status = new NodeStatusDto(
            NodeId: node.Id,
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            BatteryLevel: 100
        );

        // Act
        node.ApplyStatus(status);

        // Assert
        node.IsOnline.Should().BeTrue();
        node.BatteryLevel.Should().Be(100);
        node.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region SensorMappingExtensions Tests (v3.0 Two-Tier: Sensor has Code/Name directly)

    [Fact]
    public void Sensor_ToDto_MapsAllProperties()
    {
        // Arrange - v3.0 Two-Tier: Sensor has Code, Name, Protocol, Category directly
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
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
            LastCalibratedAt = DateTime.UtcNow.AddMonths(-1),
            CalibrationNotes = "Calibrated with reference",
            CalibrationDueAt = DateTime.UtcNow.AddMonths(5),
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = sensor.ToDto();

        // Assert
        result.Id.Should().Be(sensor.Id);
        result.TenantId.Should().Be(sensor.TenantId);
        result.Code.Should().Be("dht22-living-room");
        result.Name.Should().Be("Living Room DHT22");
        result.Protocol.Should().Be(CommunicationProtocolDto.OneWire);
        result.Category.Should().Be("climate");
        result.Description.Should().Be("Temperature and humidity sensor");
        result.SerialNumber.Should().Be("DHT22-001");
        result.IntervalSeconds.Should().Be(30);
        result.MinIntervalSeconds.Should().Be(2);
        result.OffsetCorrection.Should().Be(0.5);
        result.GainCorrection.Should().Be(1.02);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateSensorDto_ToEntity_MapsAllProperties()
    {
        // Arrange - v3.0 Two-Tier: CreateSensorDto has Code, Name, Protocol, Category
        var tenantId = Guid.NewGuid();
        var dto = new CreateSensorDto(
            Code: "bme280-kitchen",
            Name: "Kitchen Sensor",
            Protocol: CommunicationProtocolDto.I2C,
            Category: "climate",
            Description: "Sensor in kitchen",
            SerialNumber: "BME280-042",
            IntervalSeconds: 60
        );

        // Act
        var result = dto.ToEntity(tenantId);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.TenantId.Should().Be(tenantId);
        result.Code.Should().Be("bme280-kitchen");
        result.Name.Should().Be("Kitchen Sensor");
        result.Protocol.Should().Be(CommunicationProtocol.I2C);
        result.Category.Should().Be("climate");
        result.Description.Should().Be("Sensor in kitchen");
        result.SerialNumber.Should().Be("BME280-042");
        result.IntervalSeconds.Should().Be(60);
        result.OffsetCorrection.Should().Be(0);
        result.GainCorrection.Should().Be(1.0);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Sensor_ApplyUpdate_UpdatesProvidedProperties()
    {
        // Arrange - v3.0 Two-Tier: UpdateSensorDto can update name, description, etc.
        var sensor = new Sensor
        {
            Name = "Old Name",
            Description = "Old description",
            IsActive = true
        };

        var dto = new UpdateSensorDto(
            Name: "New Name",
            Description: "New description",
            IsActive: false
        );

        // Act
        sensor.ApplyUpdate(dto);

        // Assert
        sensor.Name.Should().Be("New Name");
        sensor.Description.Should().Be("New description");
        sensor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Sensor_ApplyCalibration_UpdatesCalibrationProperties()
    {
        // Arrange - v3.0 Two-Tier: CalibrateSensorDto for calibration
        var sensor = new Sensor
        {
            OffsetCorrection = 0,
            GainCorrection = 1.0,
            LastCalibratedAt = null,
            CalibrationNotes = null
        };

        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.5,
            GainCorrection: 1.02,
            CalibrationNotes: "Calibrated with reference",
            CalibrationDueAt: DateTime.UtcNow.AddMonths(6)
        );

        // Act
        sensor.ApplyCalibration(dto);

        // Assert
        sensor.OffsetCorrection.Should().Be(0.5);
        sensor.GainCorrection.Should().Be(1.02);
        sensor.CalibrationNotes.Should().Be("Calibrated with reference");
        sensor.LastCalibratedAt.Should().NotBeNull();
        sensor.LastCalibratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Sensor_ToDtos_ConvertsCollection()
    {
        // Arrange - v3.0 Two-Tier: Sensor has Code/Name directly
        var sensors = new List<Sensor>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Code = "dht22-01",
                Name = "DHT22 Sensor 1",
                Protocol = CommunicationProtocol.OneWire,
                Category = "climate",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Code = "bme280-01",
                Name = "BME280 Sensor 2",
                Protocol = CommunicationProtocol.I2C,
                Category = "climate",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = sensors.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Code.Should().Be("dht22-01");
        result[1].Code.Should().Be("bme280-01");
    }

    #endregion

    #region AlertMappingExtensions Tests

    [Fact]
    public void Alert_ToDto_WithAlertType_MapsAllProperties()
    {
        // Arrange
        var alertType = new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "mold_risk",
            Name = "Schimmelrisiko",
            DefaultLevel = AlertLevel.Warning
        };

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            HubId = Guid.NewGuid(),
            NodeId = Guid.NewGuid(),  // Changed from SensorId to NodeId
            AlertTypeId = alertType.Id,
            Level = AlertLevel.Critical,
            Message = "High humidity detected",
            Recommendation = "Open windows",
            Source = AlertSource.Cloud,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            AcknowledgedAt = null,
            IsActive = true
        };

        // Act
        var result = alert.ToDto(alertType, "Test Hub", "Test Node");

        // Assert
        result.Id.Should().Be(alert.Id);
        result.TenantId.Should().Be(alert.TenantId);
        result.HubName.Should().Be("Test Hub");
        result.NodeName.Should().Be("Test Node");
        result.AlertTypeCode.Should().Be("mold_risk");
        result.AlertTypeName.Should().Be("Schimmelrisiko");
        result.Level.Should().Be(AlertLevelDto.Critical);
        result.Message.Should().Be("High humidity detected");
        result.Recommendation.Should().Be("Open windows");
        result.Source.Should().Be(AlertSourceDto.Cloud);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Alert_ToDto_WithLoadedRelations_MapsCorrectly()
    {
        // Arrange
        var alertType = new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "frost_warning",
            Name = "Frostwarnung"
        };

        var hub = new HubEntity { Name = "Garden Hub" };
        var node = new Node { Name = "Outdoor Node" };  // Changed from Sensor to Node

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            AlertTypeId = alertType.Id,
            AlertType = alertType,
            Hub = hub,
            Node = node,  // Changed from Sensor to Node
            Level = AlertLevel.Warning,
            Source = AlertSource.Local,
            Message = "Frost warning",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = alert.ToDto();

        // Assert
        result.AlertTypeCode.Should().Be("frost_warning");
        result.HubName.Should().Be("Garden Hub");
        result.NodeName.Should().Be("Outdoor Node");
    }

    [Fact]
    public void Alert_ToDto_WithoutAlertType_ThrowsException()
    {
        // Arrange
        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            AlertType = null,
            Level = AlertLevel.Warning,
            Source = AlertSource.Local,
            Message = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        var act = () => alert.ToDto();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AlertType must be loaded*");
    }

    [Fact]
    public void CreateAlertDto_ToEntity_MapsAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var alertTypeId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();  // Changed from sensorId to nodeId

        var dto = new CreateAlertDto(
            AlertTypeCode: "battery_low",
            Level: AlertLevelDto.Warning,
            Message: "Battery is low",
            Recommendation: "Replace battery soon",
            ExpiresAt: DateTime.UtcNow.AddDays(7)
        );

        // Act
        var result = dto.ToEntity(tenantId, alertTypeId, hubId, nodeId, AlertSource.Local);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.TenantId.Should().Be(tenantId);
        result.AlertTypeId.Should().Be(alertTypeId);
        result.HubId.Should().Be(hubId);
        result.NodeId.Should().Be(nodeId);
        result.Level.Should().Be(AlertLevel.Warning);
        result.Message.Should().Be("Battery is low");
        result.Recommendation.Should().Be("Replace battery soon");
        result.Source.Should().Be(AlertSource.Local);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Alert_ToDtos_ConvertsCollection()
    {
        // Arrange
        var alertType = new AlertType { Id = Guid.NewGuid(), Code = "test", Name = "Test" };
        var alerts = new List<Alert>
        {
            new()
            {
                Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), AlertTypeId = alertType.Id, AlertType = alertType,
                Level = AlertLevel.Info, Source = AlertSource.Local, Message = "M1", IsActive = true, CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), AlertTypeId = alertType.Id, AlertType = alertType,
                Level = AlertLevel.Warning, Source = AlertSource.Cloud, Message = "M2", IsActive = true, CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = alerts.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Message.Should().Be("M1");
        result[1].Message.Should().Be("M2");
    }

    #endregion

    #region NodeMappingExtensions Additional Tests

    [Fact]
    public void NodeStatus_ToDto_MapsAllValues()
    {
        // Test all NodeStatus enum values
        NodeStatus.Unconfigured.ToDto().Should().Be(NodeProvisioningStatusDto.Unconfigured);
        NodeStatus.Pairing.ToDto().Should().Be(NodeProvisioningStatusDto.Pairing);
        NodeStatus.Configured.ToDto().Should().Be(NodeProvisioningStatusDto.Configured);
        NodeStatus.Error.ToDto().Should().Be(NodeProvisioningStatusDto.Error);
    }

    [Fact]
    public void NodeProvisioningStatusDto_ToEntity_MapsAllValues()
    {
        // Test all NodeProvisioningStatusDto enum values
        NodeProvisioningStatusDto.Unconfigured.ToEntity().Should().Be(NodeStatus.Unconfigured);
        NodeProvisioningStatusDto.Pairing.ToEntity().Should().Be(NodeStatus.Pairing);
        NodeProvisioningStatusDto.Configured.ToEntity().Should().Be(NodeStatus.Configured);
        NodeProvisioningStatusDto.Error.ToEntity().Should().Be(NodeStatus.Error);
    }

    [Fact]
    public void NodeStatus_ToDto_UnknownValue_ReturnsDefault()
    {
        // Test unknown enum value
        var unknownStatus = (NodeStatus)999;
        unknownStatus.ToDto().Should().Be(NodeProvisioningStatusDto.Unconfigured);
    }

    [Fact]
    public void NodeProvisioningStatusDto_ToEntity_UnknownValue_ReturnsDefault()
    {
        // Test unknown enum value
        var unknownStatus = (NodeProvisioningStatusDto)999;
        unknownStatus.ToEntity().Should().Be(NodeStatus.Unconfigured);
    }

    [Fact]
    public void CreateNodeDto_ToEntity_WithName_UsesProvidedName()
    {
        // Arrange
        var dto = new CreateNodeDto(NodeId: "node-01", Name: "My Custom Node");
        var hubId = Guid.NewGuid();

        // Act
        var result = dto.ToEntity(hubId);

        // Assert
        result.Name.Should().Be("My Custom Node");
        result.HubId.Should().Be(hubId);
    }

    [Fact]
    public void CreateNodeDto_ToEntity_WithoutName_GeneratesNameFromNodeId()
    {
        // Arrange
        var dto = new CreateNodeDto(NodeId: "living-room-sensor");
        var hubId = Guid.NewGuid();

        // Act
        var result = dto.ToEntity(hubId);

        // Assert
        result.Name.Should().Be("Living Room Sensor");
    }

    [Fact]
    public void CreateNodeDto_ToEntity_WithLocation_IncludesLocation()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "node-01",
            Location: new LocationDto("Kitchen", 50.0, 8.0));
        var hubId = Guid.NewGuid();

        // Act
        var result = dto.ToEntity(hubId);

        // Assert
        result.Location.Should().NotBeNull();
        result.Location!.Name.Should().Be("Kitchen");
    }

    [Fact]
    public void NodeRegistrationDto_ToEntity_GeneratesNodeIdFromMac()
    {
        // Arrange
        var dto = new NodeRegistrationDto(MacAddress: "AA:BB:CC:DD:EE:FF", FirmwareVersion: "1.0.0", Name: "Test Node");
        var hubId = Guid.NewGuid();
        var apiKeyHash = "hashed-key";

        // Act
        var result = dto.ToEntity(hubId, apiKeyHash);

        // Assert
        result.NodeId.Should().Be("node-aabbccddeeff");
        result.MacAddress.Should().Be("AA:BB:CC:DD:EE:FF");
        result.Name.Should().Be("Test Node");
        result.FirmwareVersion.Should().Be("1.0.0");
        result.ApiKeyHash.Should().Be(apiKeyHash);
        result.Status.Should().Be(NodeStatus.Configured);
    }

    [Fact]
    public void NodeRegistrationDto_ToEntity_WithoutName_GeneratesNameFromMac()
    {
        // Arrange
        var dto = new NodeRegistrationDto(MacAddress: "AA:BB:CC:DD:EE:FF");
        var hubId = Guid.NewGuid();

        // Act
        var result = dto.ToEntity(hubId, "hash");

        // Assert
        result.Name.Should().Be("Node EEFF");
    }

    [Fact]
    public void Node_ApplyUpdate_WithAllFields_UpdatesAllFields()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            Location = null,
            FirmwareVersion = "1.0",
            IsSimulation = false
        };
        var update = new UpdateNodeDto(
            Name: "New Name",
            Location: new LocationDto("Living Room"),
            FirmwareVersion: "2.0",
            IsSimulation: true);

        // Act
        node.ApplyUpdate(update);

        // Assert
        node.Name.Should().Be("New Name");
        node.Location.Should().NotBeNull();
        node.Location!.Name.Should().Be("Living Room");
        node.FirmwareVersion.Should().Be("2.0");
        node.IsSimulation.Should().BeTrue();
    }

    [Fact]
    public void Node_ApplyUpdate_WithPartialFields_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            Location = new Location("Original"),
            FirmwareVersion = "1.0",
            IsSimulation = false
        };
        var update = new UpdateNodeDto(Name: "New Name");

        // Act
        node.ApplyUpdate(update);

        // Assert
        node.Name.Should().Be("New Name");
        node.Location!.Name.Should().Be("Original"); // Unchanged
        node.FirmwareVersion.Should().Be("1.0"); // Unchanged
        node.IsSimulation.Should().BeFalse(); // Unchanged
    }

    [Fact]
    public void Node_ApplyStatus_UpdatesOnlineAndLastSeen()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsOnline = false,
            LastSeen = null,
            BatteryLevel = null
        };
        var status = new NodeStatusDto(
            NodeId: node.Id,
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            BatteryLevel: 85);

        // Act
        node.ApplyStatus(status);

        // Assert
        node.IsOnline.Should().BeTrue();
        node.LastSeen.Should().NotBeNull();
        node.BatteryLevel.Should().Be(85);
    }

    [Fact]
    public void Node_ApplyStatus_WithNullOptionals_DoesNotOverwrite()
    {
        // Arrange
        var originalLastSeen = DateTime.UtcNow.AddHours(-1);
        var node = new Node
        {
            Id = Guid.NewGuid(),
            IsOnline = true,
            LastSeen = originalLastSeen,
            BatteryLevel = 90
        };
        var status = new NodeStatusDto(
            NodeId: node.Id,
            IsOnline: false,
            LastSeen: null,
            BatteryLevel: null);

        // Act
        node.ApplyStatus(status);

        // Assert
        node.IsOnline.Should().BeFalse();
        node.LastSeen.Should().Be(originalLastSeen); // Unchanged
        node.BatteryLevel.Should().Be(90); // Unchanged
    }

    #endregion

    #region ReadingMappingExtensions Additional Tests

    [Fact]
    public void Reading_ToDto_WithCapabilityLookup_UsesCapabilityDisplayName()
    {
        // Arrange
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Code = "BME280",
            Name = "BME280 Sensor",
            Icon = "thermostat",
            Color = "#FF5722"
        };
        var capability = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            Sensor = sensor,
            MeasurementType = "temperature",
            DisplayName = "Temperatur",
            Unit = "°C"
        };
        sensor.Capabilities.Add(capability);

        var reading = new Reading
        {
            Id = 1,
            TenantId = Guid.NewGuid(),
            NodeId = Guid.NewGuid(),
            AssignmentId = null,
            MeasurementType = "temperature",
            RawValue = 21.5,
            Value = 21.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = reading.ToDto(capability);

        // Assert
        result.DisplayName.Should().Be("Temperatur");
        result.SensorCode.Should().Be("BME280");
        result.SensorIcon.Should().Be("thermostat");
    }

    [Fact]
    public void Reading_ToDto_WithoutCapability_UsesMeasurementType()
    {
        // Arrange
        var reading = new Reading
        {
            Id = 1,
            TenantId = Guid.NewGuid(),
            NodeId = Guid.NewGuid(),
            MeasurementType = "humidity",
            RawValue = 65.0,
            Value = 65.0,
            Unit = "%",
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = reading.ToDto();

        // Assert
        result.DisplayName.Should().Be("humidity");
        result.SensorCode.Should().BeEmpty();
    }

    [Fact]
    public void Reading_ToDtos_WithCapabilityLookup_AppliesLookup()
    {
        // Arrange
        var sensor = new Sensor { Id = Guid.NewGuid(), Code = "DHT22", Name = "DHT22" };
        var capability = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            Sensor = sensor,
            MeasurementType = "temperature",
            DisplayName = "Temperature",
            Unit = "°C"
        };
        var capabilityLookup = new Dictionary<string, SensorCapability>
        {
            { "temperature", capability }
        };

        var readings = new List<Reading>
        {
            new Reading
            {
                Id = 1,
                TenantId = Guid.NewGuid(),
                NodeId = Guid.NewGuid(),
                AssignmentId = null, // No assignment - will use lookup
                MeasurementType = "temperature",
                Value = 21.5,
                Unit = "°C",
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var result = readings.ToDtos(capabilityLookup).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].DisplayName.Should().Be("Temperature");
        result[0].SensorCode.Should().Be("DHT22");
    }

    [Fact]
    public void Reading_ToDtos_WithNullLookup_ReturnsBasicDtos()
    {
        // Arrange
        var readings = new List<Reading>
        {
            new Reading
            {
                Id = 1,
                TenantId = Guid.NewGuid(),
                NodeId = Guid.NewGuid(),
                MeasurementType = "temperature",
                Value = 21.5,
                Unit = "°C",
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        var result = readings.ToDtos(null).ToList();

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public void CreateReadingDto_ToEntity_AppliesCalibration()
    {
        // Arrange - CreateReadingDto: NodeId, EndpointId, MeasurementType, RawValue, HubId?, Timestamp?
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "TEMPERATURE",
            RawValue: 21.3,
            Timestamp: DateTime.UtcNow);
        var tenantId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var calibratedValue = 21.5;

        // Act
        var result = dto.ToEntity(tenantId, nodeId, assignmentId, "°C", calibratedValue);

        // Assert
        result.MeasurementType.Should().Be("temperature"); // Lowercased
        result.RawValue.Should().Be(21.3);
        result.Value.Should().Be(21.5);
        result.Unit.Should().Be("°C");
        result.AssignmentId.Should().Be(assignmentId);
    }

    [Fact]
    public void CreateReadingDto_ToEntity_WithoutTimestamp_UsesUtcNow()
    {
        // Arrange - CreateReadingDto: NodeId, EndpointId, MeasurementType, RawValue, HubId?, Timestamp?
        var dto = new CreateReadingDto(
            NodeId: "node-01",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            Timestamp: null);
        var tenantId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        // Act
        var result = dto.ToEntity(tenantId, nodeId, assignmentId, "°C", 21.5);

        // Assert
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region NodeSensorAssignmentMappingExtensions Additional Tests

    [Fact]
    public void CreateNodeSensorAssignmentDto_ToEntity_MapsAllFields()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: Guid.NewGuid(),
            EndpointId: 1,
            Alias: "Living Room Temp",
            I2CAddressOverride: "0x77",
            SdaPinOverride: 21,
            SclPinOverride: 22,
            OneWirePinOverride: 4,
            AnalogPinOverride: 34,
            DigitalPinOverride: 16,
            TriggerPinOverride: 17,
            EchoPinOverride: 18,
            IntervalSecondsOverride: 30);
        var nodeId = Guid.NewGuid();

        // Act
        var result = dto.ToEntity(nodeId);

        // Assert
        result.NodeId.Should().Be(nodeId);
        result.SensorId.Should().Be(dto.SensorId);
        result.EndpointId.Should().Be(1);
        result.Alias.Should().Be("Living Room Temp");
        result.I2CAddressOverride.Should().Be("0x77");
        result.SdaPinOverride.Should().Be(21);
        result.SclPinOverride.Should().Be(22);
        result.OneWirePinOverride.Should().Be(4);
        result.AnalogPinOverride.Should().Be(34);
        result.DigitalPinOverride.Should().Be(16);
        result.TriggerPinOverride.Should().Be(17);
        result.EchoPinOverride.Should().Be(18);
        result.IntervalSecondsOverride.Should().Be(30);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void NodeSensorAssignment_ApplyUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var assignment = new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            Alias = "Original Alias",
            I2CAddressOverride = "0x76",
            IsActive = true
        };
        var update = new UpdateNodeSensorAssignmentDto(
            Alias: "New Alias",
            IsActive: false);

        // Act
        assignment.ApplyUpdate(update);

        // Assert
        assignment.Alias.Should().Be("New Alias");
        assignment.I2CAddressOverride.Should().Be("0x76"); // Unchanged
        assignment.IsActive.Should().BeFalse();
    }

    #endregion

    #region SensorMappingExtensions Additional Tests

    [Fact]
    public void CreateSensorCapabilityDto_ShouldBeCreatedWithAllProperties()
    {
        // Arrange & Act - CreateSensorCapabilityDto has: MeasurementType, DisplayName, Unit, MinValue?, MaxValue?, Resolution, Accuracy, MatterClusterId?, MatterClusterName?, SortOrder
        var dto = new CreateSensorCapabilityDto(
            MeasurementType: "temperature",
            DisplayName: "Temperatur",
            Unit: "°C",
            MinValue: -40,
            MaxValue: 80,
            Resolution: 0.1,
            Accuracy: 0.5,
            MatterClusterId: 0x0402,
            MatterClusterName: "TemperatureMeasurement",
            SortOrder: 1);

        // Assert
        dto.MeasurementType.Should().Be("temperature");
        dto.DisplayName.Should().Be("Temperatur");
        dto.Unit.Should().Be("°C");
        dto.MinValue.Should().Be(-40);
        dto.MaxValue.Should().Be(80);
        dto.Resolution.Should().Be(0.1);
        dto.Accuracy.Should().Be(0.5);
        dto.MatterClusterId.Should().Be(0x0402u);
        dto.MatterClusterName.Should().Be("TemperatureMeasurement");
        dto.SortOrder.Should().Be(1);
    }

    [Fact]
    public void CreateSensorCapabilityDto_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var dto = new CreateSensorCapabilityDto(
            MeasurementType: "humidity",
            DisplayName: "Humidity",
            Unit: "%");

        // Assert
        dto.MinValue.Should().BeNull();
        dto.MaxValue.Should().BeNull();
        dto.Resolution.Should().Be(0.01); // Default
        dto.Accuracy.Should().Be(0.5); // Default
        dto.MatterClusterId.Should().BeNull();
        dto.MatterClusterName.Should().BeNull();
        dto.SortOrder.Should().Be(0); // Default
    }

    #endregion

    #region TenantMappingExtensions Additional Tests

    [Fact]
    public void CreateTenantDto_ToEntity_MapsAllFields()
    {
        // Arrange
        var dto = new CreateTenantDto(
            Name: "Acme Corp",
            CloudApiKey: "test-api-key");

        // Act
        var result = dto.ToEntity();

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Acme Corp");
        result.CloudApiKey.Should().Be("test-api-key");
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Tenant_ApplyUpdate_UpdatesAllFields()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Old Name",
            CloudApiKey = "old-key",
            IsActive = true
        };
        var update = new UpdateTenantDto(
            Name: "New Name",
            CloudApiKey: "new-key",
            IsActive: false);

        // Act
        tenant.ApplyUpdate(update);

        // Assert
        tenant.Name.Should().Be("New Name");
        tenant.CloudApiKey.Should().Be("new-key");
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Tenant_ApplyUpdate_WithNulls_DoesNotOverwrite()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Keep Name",
            CloudApiKey = "keep-key",
            IsActive = true
        };
        var update = new UpdateTenantDto();

        // Act
        tenant.ApplyUpdate(update);

        // Assert
        tenant.Name.Should().Be("Keep Name");
        tenant.CloudApiKey.Should().Be("keep-key");
        tenant.IsActive.Should().BeTrue();
    }

    #endregion

    #region NodeSensorAssignment ApplyUpdate Complete Coverage

    [Fact]
    public void NodeSensorAssignment_ApplyUpdate_AllPinOverrides()
    {
        // Arrange
        var assignment = new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            Alias = "Original",
            I2CAddressOverride = null,
            SdaPinOverride = null,
            SclPinOverride = null,
            OneWirePinOverride = null,
            AnalogPinOverride = null,
            DigitalPinOverride = null,
            TriggerPinOverride = null,
            EchoPinOverride = null,
            IntervalSecondsOverride = null,
            IsActive = true
        };

        var update = new UpdateNodeSensorAssignmentDto(
            Alias: "Updated Alias",
            I2CAddressOverride: "0x77",
            SdaPinOverride: 21,
            SclPinOverride: 22,
            OneWirePinOverride: 4,
            AnalogPinOverride: 34,
            DigitalPinOverride: 16,
            TriggerPinOverride: 17,
            EchoPinOverride: 18,
            IntervalSecondsOverride: 120,
            IsActive: false
        );

        // Act
        assignment.ApplyUpdate(update);

        // Assert
        assignment.Alias.Should().Be("Updated Alias");
        assignment.I2CAddressOverride.Should().Be("0x77");
        assignment.SdaPinOverride.Should().Be(21);
        assignment.SclPinOverride.Should().Be(22);
        assignment.OneWirePinOverride.Should().Be(4);
        assignment.AnalogPinOverride.Should().Be(34);
        assignment.DigitalPinOverride.Should().Be(16);
        assignment.TriggerPinOverride.Should().Be(17);
        assignment.EchoPinOverride.Should().Be(18);
        assignment.IntervalSecondsOverride.Should().Be(120);
        assignment.IsActive.Should().BeFalse();
    }

    [Fact]
    public void NodeSensorAssignment_ApplyUpdate_NullValues_DoesNotOverwrite()
    {
        // Arrange
        var assignment = new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            Alias = "Keep Alias",
            I2CAddressOverride = "0x76",
            SdaPinOverride = 21,
            SclPinOverride = 22,
            IsActive = true
        };

        var update = new UpdateNodeSensorAssignmentDto(); // All nulls

        // Act
        assignment.ApplyUpdate(update);

        // Assert
        assignment.Alias.Should().Be("Keep Alias");
        assignment.I2CAddressOverride.Should().Be("0x76");
        assignment.SdaPinOverride.Should().Be(21);
        assignment.SclPinOverride.Should().Be(22);
        assignment.IsActive.Should().BeTrue();
    }

    #endregion

    #region SensorMappingExtensions Complete Coverage

    [Fact]
    public void Sensor_ApplyUpdate_AllFields()
    {
        // Arrange
        var sensor = CreateTestSensor();
        var update = new UpdateSensorDto(
            Name: "Updated Sensor",
            Description: "Updated description",
            SerialNumber: "SN-UPDATED",
            Manufacturer: "New Manufacturer",
            Model: "New Model",
            DatasheetUrl: "https://example.com/datasheet",
            I2CAddress: "0x77",
            SdaPin: 19,
            SclPin: 20,
            OneWirePin: 5,
            AnalogPin: 35,
            DigitalPin: 15,
            TriggerPin: 14,
            EchoPin: 13,
            IntervalSeconds: 120,
            MinIntervalSeconds: 30,
            WarmupTimeMs: 200,
            OffsetCorrection: 1.5,
            GainCorrection: 1.1,
            CalibrationNotes: "Calibrated today",
            Category: "environmental",
            Icon: "new_icon",
            Color: "#00FF00",
            IsActive: false
        );

        // Act
        sensor.ApplyUpdate(update);

        // Assert
        sensor.Name.Should().Be("Updated Sensor");
        sensor.Description.Should().Be("Updated description");
        sensor.SerialNumber.Should().Be("SN-UPDATED");
        sensor.Manufacturer.Should().Be("New Manufacturer");
        sensor.Model.Should().Be("New Model");
        sensor.DatasheetUrl.Should().Be("https://example.com/datasheet");
        sensor.I2CAddress.Should().Be("0x77");
        sensor.SdaPin.Should().Be(19);
        sensor.SclPin.Should().Be(20);
        sensor.OneWirePin.Should().Be(5);
        sensor.AnalogPin.Should().Be(35);
        sensor.DigitalPin.Should().Be(15);
        sensor.TriggerPin.Should().Be(14);
        sensor.EchoPin.Should().Be(13);
        sensor.IntervalSeconds.Should().Be(120);
        sensor.MinIntervalSeconds.Should().Be(30);
        sensor.WarmupTimeMs.Should().Be(200);
        sensor.OffsetCorrection.Should().Be(1.5);
        sensor.GainCorrection.Should().Be(1.1);
        sensor.CalibrationNotes.Should().Be("Calibrated today");
        sensor.Category.Should().Be("environmental");
        sensor.Icon.Should().Be("new_icon");
        sensor.Color.Should().Be("#00FF00");
        sensor.IsActive.Should().BeFalse();
        sensor.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Sensor_ApplyUpdate_NullValues_DoesNotOverwrite()
    {
        // Arrange
        var sensor = CreateTestSensor();
        sensor.Name = "Original Name";
        sensor.Description = "Original Description";
        var originalName = sensor.Name;
        var originalDescription = sensor.Description;

        var update = new UpdateSensorDto(); // All nulls

        // Act
        sensor.ApplyUpdate(update);

        // Assert
        sensor.Name.Should().Be(originalName);
        sensor.Description.Should().Be(originalDescription);
    }

    [Fact]
    public void Sensor_ApplyCalibration_SetsAllFields()
    {
        // Arrange
        var sensor = CreateTestSensor();
        var calibrationDueAt = DateTime.UtcNow.AddDays(365);
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.5,
            GainCorrection: 1.02,
            CalibrationNotes: "Calibrated with reference sensor",
            CalibrationDueAt: calibrationDueAt
        );

        // Act
        sensor.ApplyCalibration(dto);

        // Assert
        sensor.OffsetCorrection.Should().Be(0.5);
        sensor.GainCorrection.Should().Be(1.02);
        sensor.CalibrationNotes.Should().Be("Calibrated with reference sensor");
        sensor.CalibrationDueAt.Should().Be(calibrationDueAt);
        sensor.LastCalibratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        sensor.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SensorCapabilities_ToDtos_MapsAllCapabilities()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var capabilities = new List<SensorCapability>
        {
            new SensorCapability
            {
                Id = Guid.NewGuid(),
                SensorId = sensorId,
                MeasurementType = "temperature",
                DisplayName = "Temperature",
                Unit = "°C",
                MinValue = -40,
                MaxValue = 80,
                Resolution = 0.1,
                Accuracy = 0.5,
                MatterClusterId = 0x0402,
                MatterClusterName = "TemperatureMeasurement",
                SortOrder = 0,
                IsActive = true
            },
            new SensorCapability
            {
                Id = Guid.NewGuid(),
                SensorId = sensorId,
                MeasurementType = "humidity",
                DisplayName = "Humidity",
                Unit = "%",
                MinValue = 0,
                MaxValue = 100,
                SortOrder = 1,
                IsActive = true
            }
        };

        // Act
        var result = capabilities.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].MeasurementType.Should().Be("temperature");
        result[0].MatterClusterId.Should().Be(0x0402u);
        result[1].MeasurementType.Should().Be("humidity");
    }

    [Fact]
    public void Sensors_ToDtos_MapsAllSensors()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            CreateTestSensor(),
            CreateTestSensor()
        };
        sensors[0].Code = "bme280";
        sensors[1].Code = "dht22";

        // Act
        var result = sensors.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Code.Should().Be("bme280");
        result[1].Code.Should().Be("dht22");
    }

    [Fact]
    public void CreateSensorDto_ToEntity_WithCapabilities_AssignsCorrectSortOrder()
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "BME280",
            Name: "BME280 Environmental Sensor",
            Category: "environmental",
            Protocol: CommunicationProtocolDto.I2C,
            Capabilities: new List<CreateSensorCapabilityDto>
            {
                new CreateSensorCapabilityDto("temperature", "Temperature", "°C"),
                new CreateSensorCapabilityDto("humidity", "Humidity", "%"),
                new CreateSensorCapabilityDto("pressure", "Pressure", "hPa", SortOrder: 5) // Explicit sort
            }
        );

        // Act
        var result = dto.ToEntity(Guid.NewGuid());

        // Assert
        result.Capabilities.Should().HaveCount(3);
        var sortedCapabilities = result.Capabilities.OrderBy(c => c.SortOrder).ToList();
        sortedCapabilities[0].SortOrder.Should().Be(0);
        sortedCapabilities[1].SortOrder.Should().Be(1);
        sortedCapabilities[2].SortOrder.Should().Be(5); // Explicit sort preserved
    }

    [Fact]
    public void CreateSensorDto_ToEntity_WithoutCapabilities_ReturnsEmptyCollection()
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "BASIC",
            Name: "Basic Sensor",
            Category: "test",
            Protocol: CommunicationProtocolDto.Digital,
            Capabilities: null
        );

        // Act
        var result = dto.ToEntity(Guid.NewGuid());

        // Assert
        result.Capabilities.Should().BeEmpty();
    }

    [Fact]
    public void Sensor_ApplyUpdate_WithNewCapabilities_AddsCapabilitiesToCollection()
    {
        // Arrange - Sensor without capabilities
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "test-sensor",
            Name = "Test Sensor",
            Category = "test",
            Protocol = CommunicationProtocol.I2C,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Capabilities = new List<SensorCapability>() // Empty initially
        };

        // Update with new capabilities (id=null means new)
        var updateDto = new UpdateSensorDto(
            Capabilities: new List<UpdateSensorCapabilityDto>
            {
                new UpdateSensorCapabilityDto(
                    Id: null, // NEW capability
                    MeasurementType: "temperature",
                    DisplayName: "Temperatur",
                    Unit: "°C",
                    Resolution: 0.1,
                    Accuracy: 0.5
                ),
                new UpdateSensorCapabilityDto(
                    Id: null, // NEW capability
                    MeasurementType: "humidity",
                    DisplayName: "Luftfeuchtigkeit",
                    Unit: "%"
                )
            }
        );

        // Act
        sensor.ApplyUpdate(updateDto);

        // Assert
        sensor.Capabilities.Should().HaveCount(2);
        sensor.Capabilities.Should().Contain(c => c.MeasurementType == "temperature");
        sensor.Capabilities.Should().Contain(c => c.MeasurementType == "humidity");
        // New capabilities should have generated IDs
        sensor.Capabilities.Should().OnlyContain(c => c.Id != Guid.Empty);
    }

    [Fact]
    public void Sensor_ApplyUpdate_WithExistingAndNewCapabilities_UpdatesExistingAndAddsNew()
    {
        // Arrange - Sensor with one existing capability
        var sensorId = Guid.NewGuid();
        var existingCapabilityId = Guid.NewGuid();
        var sensor = new Sensor
        {
            Id = sensorId,
            TenantId = Guid.NewGuid(),
            Code = "test-sensor",
            Name = "Test Sensor",
            Category = "test",
            Protocol = CommunicationProtocol.I2C,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Capabilities = new List<SensorCapability>
            {
                new SensorCapability
                {
                    Id = existingCapabilityId,
                    SensorId = sensorId,
                    MeasurementType = "temperature",
                    DisplayName = "Alte Temperatur",
                    Unit = "°C",
                    IsActive = true
                }
            }
        };

        // Update: modify existing capability + add new one
        var updateDto = new UpdateSensorDto(
            Capabilities: new List<UpdateSensorCapabilityDto>
            {
                new UpdateSensorCapabilityDto(
                    Id: existingCapabilityId, // UPDATE existing
                    DisplayName: "Neue Temperatur" // Changed display name
                ),
                new UpdateSensorCapabilityDto(
                    Id: null, // NEW capability
                    MeasurementType: "humidity",
                    DisplayName: "Luftfeuchtigkeit",
                    Unit: "%"
                )
            }
        );

        // Act
        sensor.ApplyUpdate(updateDto);

        // Assert
        sensor.Capabilities.Should().HaveCount(2);

        // Existing capability was updated
        var tempCapability = sensor.Capabilities.First(c => c.Id == existingCapabilityId);
        tempCapability.DisplayName.Should().Be("Neue Temperatur");

        // New capability was added
        sensor.Capabilities.Should().Contain(c => c.MeasurementType == "humidity");
    }

    [Fact]
    public void Sensor_ApplyUpdate_DoesNotRemoveCapabilities_RemovalHandledByService()
    {
        // Arrange - Sensor with two capabilities
        // NOTE: ApplyUpdate intentionally does NOT remove capabilities.
        // Removal is handled by SensorService.UpdateAsync to properly manage EF Core's change tracker.
        var sensorId = Guid.NewGuid();
        var capabilityToKeepId = Guid.NewGuid();
        var capabilityNotInUpdateId = Guid.NewGuid();
        var sensor = new Sensor
        {
            Id = sensorId,
            TenantId = Guid.NewGuid(),
            Code = "test-sensor",
            Name = "Test Sensor",
            Category = "test",
            Protocol = CommunicationProtocol.I2C,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Capabilities = new List<SensorCapability>
            {
                new SensorCapability
                {
                    Id = capabilityToKeepId,
                    SensorId = sensorId,
                    MeasurementType = "temperature",
                    DisplayName = "Temperatur",
                    Unit = "°C",
                    IsActive = true
                },
                new SensorCapability
                {
                    Id = capabilityNotInUpdateId,
                    SensorId = sensorId,
                    MeasurementType = "humidity",
                    DisplayName = "Luftfeuchtigkeit",
                    Unit = "%",
                    IsActive = true
                }
            }
        };

        // Update: only include one capability (but ApplyUpdate won't remove the other)
        var updateDto = new UpdateSensorDto(
            Capabilities: new List<UpdateSensorCapabilityDto>
            {
                new UpdateSensorCapabilityDto(
                    Id: capabilityToKeepId
                )
            }
        );

        // Act
        sensor.ApplyUpdate(updateDto);

        // Assert - Both capabilities should still exist (removal is handled by service layer)
        sensor.Capabilities.Should().HaveCount(2);
        sensor.Capabilities.Should().Contain(c => c.Id == capabilityToKeepId);
        sensor.Capabilities.Should().Contain(c => c.Id == capabilityNotInUpdateId);
    }

    private static Sensor CreateTestSensor()
    {
        return new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            Code = "test-sensor",
            Name = "Test Sensor",
            Category = "test",
            Protocol = CommunicationProtocol.I2C,
            I2CAddress = "0x76",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
