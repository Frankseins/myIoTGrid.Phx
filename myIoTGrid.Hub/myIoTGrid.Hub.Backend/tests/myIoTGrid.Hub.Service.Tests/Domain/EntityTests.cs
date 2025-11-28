using FluentAssertions;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.ValueObjects;
using HubEntity = myIoTGrid.Hub.Domain.Entities.Hub;

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
        node.Sensors.Should().NotBeNull();
        node.Sensors.Should().BeEmpty();
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
        // Arrange
        var node = new Node();
        var sensor = new Sensor { Id = Guid.NewGuid() };
        var reading = new Reading { Id = 1 };

        // Act
        node.Sensors.Add(sensor);
        node.Readings.Add(reading);

        // Assert
        node.Sensors.Should().HaveCount(1);
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

    #region Sensor Entity Tests (Physical sensor chip = Matter Endpoint)

    [Fact]
    public void Sensor_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var sensor = new Sensor();

        // Assert
        sensor.Id.Should().Be(Guid.Empty);
        sensor.NodeId.Should().Be(Guid.Empty);
        sensor.SensorTypeId.Should().BeEmpty();
        sensor.EndpointId.Should().Be(0);
        sensor.Name.Should().BeNull();
        sensor.IsActive.Should().BeTrue();
        sensor.CreatedAt.Should().Be(default);
        sensor.Node.Should().BeNull();
        sensor.SensorType.Should().BeNull();
    }

    [Fact]
    public void Sensor_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var sensor = new Sensor
        {
            Id = id,
            NodeId = nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            Name = "Living Room Temperature",
            IsActive = true,
            CreatedAt = createdAt
        };

        // Assert
        sensor.Id.Should().Be(id);
        sensor.NodeId.Should().Be(nodeId);
        sensor.SensorTypeId.Should().Be("temperature");
        sensor.EndpointId.Should().Be(1);
        sensor.Name.Should().Be("Living Room Temperature");
        sensor.IsActive.Should().BeTrue();
        sensor.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Sensor_EndpointId_ShouldSupportMultipleEndpoints()
    {
        // Arrange & Act - Multiple sensors on same node with different EndpointIds
        var sensor1 = new Sensor { EndpointId = 1, SensorTypeId = "temperature" };
        var sensor2 = new Sensor { EndpointId = 2, SensorTypeId = "humidity" };
        var sensor3 = new Sensor { EndpointId = 3, SensorTypeId = "pressure" };

        // Assert
        sensor1.EndpointId.Should().Be(1);
        sensor2.EndpointId.Should().Be(2);
        sensor3.EndpointId.Should().Be(3);
    }

    #endregion

    #region Reading Entity Tests (= Matter Attribute Report)

    [Fact]
    public void Reading_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var reading = new Reading();

        // Assert
        reading.Id.Should().Be(0);  // long type, default is 0
        reading.TenantId.Should().Be(Guid.Empty);
        reading.NodeId.Should().Be(Guid.Empty);
        reading.SensorTypeId.Should().BeEmpty();
        reading.Value.Should().Be(0);
        reading.Timestamp.Should().Be(default);
        reading.IsSyncedToCloud.Should().BeFalse();
        reading.Node.Should().BeNull();
        reading.SensorType.Should().BeNull();
    }

    [Fact]
    public void Reading_ShouldSetAllProperties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var reading = new Reading
        {
            Id = 12345,
            TenantId = tenantId,
            NodeId = nodeId,
            SensorTypeId = "temperature",
            Value = 23.5,
            Timestamp = timestamp,
            IsSyncedToCloud = true
        };

        // Assert
        reading.Id.Should().Be(12345);
        reading.TenantId.Should().Be(tenantId);
        reading.NodeId.Should().Be(nodeId);
        reading.SensorTypeId.Should().Be("temperature");
        reading.Value.Should().Be(23.5);
        reading.Timestamp.Should().Be(timestamp);
        reading.IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public void Reading_Value_ShouldAcceptNegativeValues()
    {
        // Arrange & Act
        var reading = new Reading { Value = -40.0 };

        // Assert
        reading.Value.Should().Be(-40.0);
    }

    [Fact]
    public void Reading_Value_ShouldAcceptDecimalValues()
    {
        // Arrange & Act
        var reading = new Reading { Value = 21.123456789 };

        // Assert
        reading.Value.Should().BeApproximately(21.123456789, 0.0000001);
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

    #region SensorType Entity Tests (with Matter Cluster IDs)

    [Fact]
    public void SensorType_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var sensorType = new SensorType();

        // Assert
        sensorType.TypeId.Should().BeEmpty();
        sensorType.DisplayName.Should().BeEmpty();
        sensorType.Unit.Should().BeEmpty();
        sensorType.ClusterId.Should().Be(0);
        sensorType.MatterClusterName.Should().BeNull();
        sensorType.Resolution.Should().Be(0.1);
        sensorType.MinValue.Should().BeNull();
        sensorType.MaxValue.Should().BeNull();
        sensorType.Description.Should().BeNull();
        sensorType.IsCustom.Should().BeFalse();
        sensorType.Category.Should().Be("other");
        sensorType.Icon.Should().BeNull();
        sensorType.Color.Should().BeNull();
        sensorType.IsGlobal.Should().BeFalse();
        sensorType.CreatedAt.Should().Be(default);
        sensorType.Sensors.Should().NotBeNull();
        sensorType.Sensors.Should().BeEmpty();
        sensorType.Readings.Should().NotBeNull();
        sensorType.Readings.Should().BeEmpty();
    }

    [Fact]
    public void SensorType_ShouldSetAllProperties()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        // Act
        var sensorType = new SensorType
        {
            TypeId = "temperature",
            DisplayName = "Temperatur",
            Unit = "°C",
            ClusterId = 0x0402, // TemperatureMeasurement cluster
            MatterClusterName = "TemperatureMeasurement",
            Resolution = 0.1,
            MinValue = -40,
            MaxValue = 125,
            Description = "Temperatur-Messung",
            IsCustom = false,
            Category = "weather",
            Icon = "thermostat",
            Color = "#FF5722",
            IsGlobal = true,
            CreatedAt = createdAt
        };

        // Assert
        sensorType.TypeId.Should().Be("temperature");
        sensorType.DisplayName.Should().Be("Temperatur");
        sensorType.Unit.Should().Be("°C");
        sensorType.ClusterId.Should().Be(0x0402);
        sensorType.MatterClusterName.Should().Be("TemperatureMeasurement");
        sensorType.Resolution.Should().Be(0.1);
        sensorType.MinValue.Should().Be(-40);
        sensorType.MaxValue.Should().Be(125);
        sensorType.Description.Should().Be("Temperatur-Messung");
        sensorType.IsCustom.Should().BeFalse();
        sensorType.Category.Should().Be("weather");
        sensorType.Icon.Should().Be("thermostat");
        sensorType.Color.Should().Be("#FF5722");
        sensorType.IsGlobal.Should().BeTrue();
        sensorType.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SensorType_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange
        var sensorType = new SensorType();
        var sensor = new Sensor { Id = Guid.NewGuid() };
        var reading = new Reading { Id = 1 };

        // Act
        sensorType.Sensors.Add(sensor);
        sensorType.Readings.Add(reading);

        // Assert
        sensorType.Sensors.Should().HaveCount(1);
        sensorType.Readings.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("temperature", 0x0402u)]
    [InlineData("humidity", 0x0405u)]
    [InlineData("pressure", 0x0403u)]
    [InlineData("occupancy", 0x0406u)]
    public void SensorType_ClusterId_ShouldSupportKnownClusters(string typeId, uint clusterId)
    {
        // Arrange & Act
        var sensorType = new SensorType
        {
            TypeId = typeId,
            ClusterId = clusterId
        };

        // Assert
        sensorType.ClusterId.Should().Be(clusterId);
    }

    [Fact]
    public void SensorType_TypeId_IsPrimaryKey()
    {
        // Arrange & Act
        var sensorType = new SensorType
        {
            TypeId = "temperature"
        };

        // Assert - TypeId is the primary key, not a Guid Id
        sensorType.TypeId.Should().Be("temperature");
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
    public void Node_Sensor_Relationship_ShouldWork()
    {
        // Arrange
        var node = new Node { Id = Guid.NewGuid(), NodeId = "test-node" };
        var sensor = new Sensor { Id = Guid.NewGuid(), NodeId = node.Id, Node = node };

        // Act
        node.Sensors.Add(sensor);

        // Assert
        node.Sensors.Should().Contain(sensor);
        sensor.Node.Should().Be(node);
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
    public void SensorType_Sensor_Relationship_ShouldWork()
    {
        // Arrange
        var sensorType = new SensorType { TypeId = "temperature" };
        var sensor = new Sensor { Id = Guid.NewGuid(), SensorTypeId = sensorType.TypeId, SensorType = sensorType };

        // Act
        sensorType.Sensors.Add(sensor);

        // Assert
        sensorType.Sensors.Should().Contain(sensor);
        sensor.SensorType.Should().Be(sensorType);
    }

    [Fact]
    public void SensorType_Reading_Relationship_ShouldWork()
    {
        // Arrange
        var sensorType = new SensorType { TypeId = "temperature" };
        var reading = new Reading { Id = 1, SensorTypeId = sensorType.TypeId, SensorType = sensorType };

        // Act
        sensorType.Readings.Add(reading);

        // Assert
        sensorType.Readings.Should().Contain(reading);
        reading.SensorType.Should().Be(sensorType);
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
}
