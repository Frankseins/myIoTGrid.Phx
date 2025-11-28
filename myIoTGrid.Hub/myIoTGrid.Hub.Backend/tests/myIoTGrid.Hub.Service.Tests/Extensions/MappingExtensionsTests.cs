using FluentAssertions;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.ValueObjects;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

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
        var hub = new myIoTGrid.Hub.Domain.Entities.Hub
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
        result.LastSeen.Should().Be(hub.LastSeen);
        result.IsOnline.Should().BeTrue();
        result.SensorCount.Should().Be(2);  // SensorCount is actually NodeCount
    }

    [Fact]
    public void Hub_ToDto_WithNodeCount_MapsCorrectly()
    {
        // Arrange
        var hub = new myIoTGrid.Hub.Domain.Entities.Hub
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
        var hub = new myIoTGrid.Hub.Domain.Entities.Hub { Name = "Old Name" };
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
        var hub = new myIoTGrid.Hub.Domain.Entities.Hub { Description = "Old description" };
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
        var hub = new myIoTGrid.Hub.Domain.Entities.Hub
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
        var hubs = new List<myIoTGrid.Hub.Domain.Entities.Hub>
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
            Sensors = new List<Sensor>()
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

    #region SensorMappingExtensions Tests (Physical Sensor Chip)

    [Fact]
    public void Sensor_ToDto_MapsAllProperties()
    {
        // Arrange
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = Guid.NewGuid(),
            SensorTypeId = "temperature",
            EndpointId = 1,
            Name = "Temperature Sensor",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        // Act
        var result = sensor.ToDto();

        // Assert
        result.Id.Should().Be(sensor.Id);
        result.NodeId.Should().Be(sensor.NodeId);
        result.SensorTypeId.Should().Be("temperature");
        result.EndpointId.Should().Be(1);
        result.Name.Should().Be("Temperature Sensor");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CreateSensorDto_ToEntity_MapsAllProperties()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var dto = new CreateSensorDto(
            SensorTypeId: "humidity",
            EndpointId: 2,
            Name: "Humidity Sensor"
        );

        // Act
        var result = dto.ToEntity(nodeId);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.NodeId.Should().Be(nodeId);
        result.SensorTypeId.Should().Be("humidity");
        result.EndpointId.Should().Be(2);
        result.Name.Should().Be("Humidity Sensor");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Sensor_ApplyUpdate_UpdatesProvidedProperties()
    {
        // Arrange
        var sensor = new Sensor
        {
            Name = "Old Name",
            IsActive = true
        };

        var dto = new UpdateSensorDto(
            Name: "New Name",
            IsActive: false
        );

        // Act
        sensor.ApplyUpdate(dto);

        // Assert
        sensor.Name.Should().Be("New Name");
        sensor.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Sensor_ToDtos_ConvertsCollection()
    {
        // Arrange
        var sensors = new List<Sensor>
        {
            new() { Id = Guid.NewGuid(), NodeId = Guid.NewGuid(), SensorTypeId = "temperature", EndpointId = 1, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), NodeId = Guid.NewGuid(), SensorTypeId = "humidity", EndpointId = 2, CreatedAt = DateTime.UtcNow }
        };

        // Act
        var result = sensors.ToDtos().ToList();

        // Assert
        result.Should().HaveCount(2);
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

        var hub = new myIoTGrid.Hub.Domain.Entities.Hub { Name = "Garden Hub" };
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
}
