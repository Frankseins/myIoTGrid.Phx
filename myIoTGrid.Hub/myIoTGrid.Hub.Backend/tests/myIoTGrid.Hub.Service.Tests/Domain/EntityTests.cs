using FluentAssertions;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.ValueObjects;
using HubEntity = myIoTGrid.Hub.Domain.Entities.Hub;

namespace myIoTGrid.Hub.Service.Tests.Domain;

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
        hub.Sensors.Should().NotBeNull();
        hub.Sensors.Should().BeEmpty();
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
        var sensor = new Sensor { Id = Guid.NewGuid() };
        var alert = new Alert { Id = Guid.NewGuid() };

        // Act
        hub.Sensors.Add(sensor);
        hub.Alerts.Add(alert);

        // Assert
        hub.Sensors.Should().HaveCount(1);
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

    #region Sensor Entity Tests

    [Fact]
    public void Sensor_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var sensor = new Sensor();

        // Assert
        sensor.Id.Should().Be(Guid.Empty);
        sensor.HubId.Should().Be(Guid.Empty);
        sensor.SensorId.Should().BeEmpty();
        sensor.Name.Should().BeEmpty();
        sensor.Protocol.Should().Be(Protocol.WLAN);
        sensor.Location.Should().BeNull();
        sensor.SensorTypes.Should().NotBeNull();
        sensor.SensorTypes.Should().BeEmpty();
        sensor.LastSeen.Should().BeNull();
        sensor.IsOnline.Should().BeFalse();
        sensor.FirmwareVersion.Should().BeNull();
        sensor.BatteryLevel.Should().BeNull();
        sensor.CreatedAt.Should().Be(default);
        sensor.Hub.Should().BeNull();
        sensor.SensorData.Should().NotBeNull();
        sensor.SensorData.Should().BeEmpty();
    }

    [Fact]
    public void Sensor_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var lastSeen = DateTime.UtcNow.AddMinutes(-2);
        var location = new Location("Wohnzimmer", 50.0, 6.0);

        // Act
        var sensor = new Sensor
        {
            Id = id,
            HubId = hubId,
            SensorId = "sensor-01",
            Name = "Living Room Sensor",
            Protocol = Protocol.LoRaWAN,
            Location = location,
            SensorTypes = ["temperature", "humidity"],
            LastSeen = lastSeen,
            IsOnline = true,
            FirmwareVersion = "2.0.1",
            BatteryLevel = 85,
            CreatedAt = createdAt
        };

        // Assert
        sensor.Id.Should().Be(id);
        sensor.HubId.Should().Be(hubId);
        sensor.SensorId.Should().Be("sensor-01");
        sensor.Name.Should().Be("Living Room Sensor");
        sensor.Protocol.Should().Be(Protocol.LoRaWAN);
        sensor.Location.Should().Be(location);
        sensor.SensorTypes.Should().Contain("temperature");
        sensor.SensorTypes.Should().Contain("humidity");
        sensor.LastSeen.Should().Be(lastSeen);
        sensor.IsOnline.Should().BeTrue();
        sensor.FirmwareVersion.Should().Be("2.0.1");
        sensor.BatteryLevel.Should().Be(85);
        sensor.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void Sensor_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange
        var sensor = new Sensor();
        var sensorData = new SensorData { Id = Guid.NewGuid() };

        // Act
        sensor.SensorData.Add(sensorData);

        // Assert
        sensor.SensorData.Should().HaveCount(1);
    }

    [Fact]
    public void Sensor_SensorTypes_ShouldBeModifiable()
    {
        // Arrange
        var sensor = new Sensor();

        // Act
        sensor.SensorTypes.Add("temperature");
        sensor.SensorTypes.Add("humidity");

        // Assert
        sensor.SensorTypes.Should().HaveCount(2);
        sensor.SensorTypes.Should().Contain("temperature");
    }

    [Fact]
    public void Sensor_BatteryLevel_ShouldAcceptZeroToHundred()
    {
        // Arrange & Act
        var sensorZero = new Sensor { BatteryLevel = 0 };
        var sensorFull = new Sensor { BatteryLevel = 100 };
        var sensorMid = new Sensor { BatteryLevel = 50 };

        // Assert
        sensorZero.BatteryLevel.Should().Be(0);
        sensorFull.BatteryLevel.Should().Be(100);
        sensorMid.BatteryLevel.Should().Be(50);
    }

    #endregion

    #region SensorData Entity Tests

    [Fact]
    public void SensorData_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var sensorData = new SensorData();

        // Assert
        sensorData.Id.Should().Be(Guid.Empty);
        sensorData.TenantId.Should().Be(Guid.Empty);
        sensorData.SensorId.Should().Be(Guid.Empty);
        sensorData.SensorTypeId.Should().Be(Guid.Empty);
        sensorData.Value.Should().Be(0);
        sensorData.Timestamp.Should().Be(default);
        sensorData.IsSyncedToCloud.Should().BeFalse();
        sensorData.Sensor.Should().BeNull();
        sensorData.SensorType.Should().BeNull();
    }

    [Fact]
    public void SensorData_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var sensorTypeId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        // Act
        var sensorData = new SensorData
        {
            Id = id,
            TenantId = tenantId,
            SensorId = sensorId,
            SensorTypeId = sensorTypeId,
            Value = 23.5,
            Timestamp = timestamp,
            IsSyncedToCloud = true
        };

        // Assert
        sensorData.Id.Should().Be(id);
        sensorData.TenantId.Should().Be(tenantId);
        sensorData.SensorId.Should().Be(sensorId);
        sensorData.SensorTypeId.Should().Be(sensorTypeId);
        sensorData.Value.Should().Be(23.5);
        sensorData.Timestamp.Should().Be(timestamp);
        sensorData.IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public void SensorData_Value_ShouldAcceptNegativeValues()
    {
        // Arrange & Act
        var sensorData = new SensorData { Value = -40.0 };

        // Assert
        sensorData.Value.Should().Be(-40.0);
    }

    [Fact]
    public void SensorData_Value_ShouldAcceptDecimalValues()
    {
        // Arrange & Act
        var sensorData = new SensorData { Value = 21.123456789 };

        // Assert
        sensorData.Value.Should().BeApproximately(21.123456789, 0.0000001);
    }

    #endregion

    #region SensorType Entity Tests

    [Fact]
    public void SensorType_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var sensorType = new SensorType();

        // Assert
        sensorType.Id.Should().Be(Guid.Empty);
        sensorType.Code.Should().BeEmpty();
        sensorType.Name.Should().BeEmpty();
        sensorType.Unit.Should().BeEmpty();
        sensorType.Description.Should().BeNull();
        sensorType.IconName.Should().BeNull();
        sensorType.IsGlobal.Should().BeFalse();
        sensorType.CreatedAt.Should().Be(default);
        sensorType.SensorData.Should().NotBeNull();
        sensorType.SensorData.Should().BeEmpty();
    }

    [Fact]
    public void SensorType_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var sensorType = new SensorType
        {
            Id = id,
            Code = "temperature",
            Name = "Temperatur",
            Unit = "°C",
            Description = "Temperatur-Messung",
            IconName = "thermostat",
            IsGlobal = true,
            CreatedAt = createdAt
        };

        // Assert
        sensorType.Id.Should().Be(id);
        sensorType.Code.Should().Be("temperature");
        sensorType.Name.Should().Be("Temperatur");
        sensorType.Unit.Should().Be("°C");
        sensorType.Description.Should().Be("Temperatur-Messung");
        sensorType.IconName.Should().Be("thermostat");
        sensorType.IsGlobal.Should().BeTrue();
        sensorType.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void SensorType_NavigationProperties_ShouldAllowAddingItems()
    {
        // Arrange
        var sensorType = new SensorType();
        var sensorData = new SensorData { Id = Guid.NewGuid() };

        // Act
        sensorType.SensorData.Add(sensorData);

        // Assert
        sensorType.SensorData.Should().HaveCount(1);
    }

    #endregion

    #region Alert Entity Tests

    [Fact]
    public void Alert_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var alert = new Alert();

        // Assert
        alert.Id.Should().Be(Guid.Empty);
        alert.TenantId.Should().Be(Guid.Empty);
        alert.HubId.Should().BeNull();
        alert.SensorId.Should().BeNull();
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
        alert.Sensor.Should().BeNull();
        alert.AlertType.Should().BeNull();
    }

    [Fact]
    public void Alert_ShouldSetAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var hubId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
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
            SensorId = sensorId,
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
        alert.SensorId.Should().Be(sensorId);
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
    public void Hub_Sensor_Relationship_ShouldWork()
    {
        // Arrange
        var hub = new HubEntity { Id = Guid.NewGuid(), Name = "Test Hub" };
        var sensor = new Sensor { Id = Guid.NewGuid(), HubId = hub.Id, Hub = hub };

        // Act
        hub.Sensors.Add(sensor);

        // Assert
        hub.Sensors.Should().Contain(sensor);
        sensor.Hub.Should().Be(hub);
    }

    [Fact]
    public void Sensor_SensorData_Relationship_ShouldWork()
    {
        // Arrange
        var sensor = new Sensor { Id = Guid.NewGuid() };
        var data = new SensorData { Id = Guid.NewGuid(), SensorId = sensor.Id, Sensor = sensor };

        // Act
        sensor.SensorData.Add(data);

        // Assert
        sensor.SensorData.Should().Contain(data);
        data.Sensor.Should().Be(sensor);
    }

    [Fact]
    public void SensorType_SensorData_Relationship_ShouldWork()
    {
        // Arrange
        var sensorType = new SensorType { Id = Guid.NewGuid(), Code = "temperature" };
        var data = new SensorData { Id = Guid.NewGuid(), SensorTypeId = sensorType.Id, SensorType = sensorType };

        // Act
        sensorType.SensorData.Add(data);

        // Assert
        sensorType.SensorData.Should().Contain(data);
        data.SensorType.Should().Be(sensorType);
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

    #endregion
}
