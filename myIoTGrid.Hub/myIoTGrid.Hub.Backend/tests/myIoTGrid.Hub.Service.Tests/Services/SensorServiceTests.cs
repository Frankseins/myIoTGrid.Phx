using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for SensorService (Physical sensor chips = Matter Endpoints).
/// The new Sensor entity represents physical sensor chips (DHT22, BME280, etc.)
/// mounted on a Node (ESP32/LoRa32 device).
/// </summary>
public class SensorServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly SensorService _sut;
    private readonly Mock<ILogger<SensorService>> _loggerMock;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId;
    private readonly Guid _nodeId;

    public SensorServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<SensorService>>();
        var unitOfWork = new UnitOfWork(_context);

        // Create a Hub
        _hubId = Guid.NewGuid();
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = _hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });

        // Create a Node
        _nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = _nodeId,
            HubId = _hubId,
            NodeId = "test-node",
            Name = "Test Node",
            CreatedAt = DateTime.UtcNow
        });

        // Create SensorTypes (TypeId is primary key, not Id)
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "temperature",
            DisplayName = "Temperatur",
            Unit = "°C",
            ClusterId = 0x0402,  // TemperatureMeasurement
            MatterClusterName = "TemperatureMeasurement",
            Category = "weather",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "humidity",
            DisplayName = "Luftfeuchtigkeit",
            Unit = "%",
            ClusterId = 0x0405,  // RelativeHumidityMeasurement
            MatterClusterName = "RelativeHumidityMeasurement",
            Category = "weather",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "pressure",
            DisplayName = "Luftdruck",
            Unit = "hPa",
            ClusterId = 0x0403,  // PressureMeasurement
            MatterClusterName = "PressureMeasurement",
            Category = "weather",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();

        _sut = new SensorService(_context, unitOfWork, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByNodeAsync_WhenNoSensors_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetByNodeAsync(_nodeId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByNodeAsync_ReturnsOnlyNodesSensors()
    {
        // Arrange
        var otherNodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = otherNodeId,
            HubId = _hubId,
            NodeId = "other-node",
            Name = "Other Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = otherNodeId, // Different Node
            SensorTypeId = "humidity",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId);

        // Assert
        result.Should().ContainSingle();
        result.First().SensorTypeId.Should().Be("temperature");
    }

    [Fact]
    public async Task GetByNodeAsync_OrdersByEndpointId()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "humidity",
            EndpointId = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByNodeAsync(_nodeId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].EndpointId.Should().Be(1);
        result[1].EndpointId.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingSensor_ReturnsSensor()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            Name = "Living Room Temp",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(sensorId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Living Room Temp");
        result.SensorTypeId.Should().Be("temperature");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingSensor_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySensorTypeAsync_WithExistingSensor_ReturnsSensor()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetBySensorTypeAsync(_nodeId, "temperature");

        // Assert
        result.Should().NotBeNull();
        result!.SensorTypeId.Should().Be("temperature");
    }

    [Fact]
    public async Task GetBySensorTypeAsync_WithNonExistingSensorType_ReturnsNull()
    {
        // Act
        var result = await _sut.GetBySensorTypeAsync(_nodeId, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBySensorTypeAsync_CaseInsensitive()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetBySensorTypeAsync(_nodeId, "TEMPERATURE");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSensor()
    {
        // Arrange
        var dto = new CreateSensorDto(
            SensorTypeId: "temperature",
            EndpointId: 1,
            Name: "Kitchen Temperature"
        );

        // Act
        var result = await _sut.CreateAsync(_nodeId, dto);

        // Assert
        result.Should().NotBeNull();
        result.SensorTypeId.Should().Be("temperature");
        result.EndpointId.Should().Be(1);
        result.Name.Should().Be("Kitchen Temperature");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEndpointId_ThrowsException()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateSensorDto(
            SensorTypeId: "humidity",
            EndpointId: 1, // Duplicate EndpointId!
            Name: "Humidity"
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(_nodeId, dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingSensor_UpdatesSensor()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            Name = "Original Name",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateSensorDto(
            Name: "Updated Name",
            IsActive: false
        );

        // Act
        var result = await _sut.UpdateAsync(sensorId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingSensor_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateSensorDto(Name: "Test");

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithExistingSensor_DeletesAndReturnsTrue()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(sensorId);

        // Assert
        result.Should().BeTrue();
        var sensor = await _sut.GetByIdAsync(sensorId);
        sensor.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingSensor_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SyncSensorsAsync_WithNewSensorTypes_CreatesSensors()
    {
        // Arrange
        var sensorTypeIds = new[] { "temperature", "humidity" };

        // Act
        var result = await _sut.SyncSensorsAsync(_nodeId, sensorTypeIds);

        // Assert
        result.Should().HaveCount(2);
        result.Select(s => s.SensorTypeId).Should().Contain("temperature");
        result.Select(s => s.SensorTypeId).Should().Contain("humidity");
    }

    [Fact]
    public async Task SyncSensorsAsync_WithExistingSensorType_DoesNotDuplicate()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var sensorTypeIds = new[] { "temperature", "humidity" };

        // Act
        var result = await _sut.SyncSensorsAsync(_nodeId, sensorTypeIds);

        // Assert
        result.Should().HaveCount(2);
        result.Where(s => s.SensorTypeId == "temperature").Should().ContainSingle();
    }

    [Fact]
    public async Task SyncSensorsAsync_AssignsIncrementalEndpointIds()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 3, // Start at 3
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var sensorTypeIds = new[] { "humidity", "pressure" };

        // Act
        var result = (await _sut.SyncSensorsAsync(_nodeId, sensorTypeIds)).ToList();

        // Assert
        result.Should().HaveCount(3);
        var humiditySensor = result.First(s => s.SensorTypeId == "humidity");
        var pressureSensor = result.First(s => s.SensorTypeId == "pressure");
        humiditySensor.EndpointId.Should().Be(4);
        pressureSensor.EndpointId.Should().Be(5);
    }

    [Fact]
    public async Task SyncSensorsAsync_WithEmptyList_ReturnsExistingSensors()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.SyncSensorsAsync(_nodeId, Array.Empty<string>());

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task SyncSensorsAsync_CaseInsensitiveSensorTypeIds()
    {
        // Arrange - Create sensor with lowercase TypeId
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - Sync with uppercase TypeId
        var result = await _sut.SyncSensorsAsync(_nodeId, new[] { "TEMPERATURE" });

        // Assert - Should not create duplicate
        result.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByNodeAsync_IncludesSensorTypeInformation()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorTypeId = "temperature",
            EndpointId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByNodeAsync(_nodeId)).First();

        // Assert - SensorType information is included via the SensorType property
        result.SensorType.Should().NotBeNull();
        result.SensorType!.DisplayName.Should().Be("Temperatur");
        result.SensorType.Unit.Should().Be("°C");
    }
}
