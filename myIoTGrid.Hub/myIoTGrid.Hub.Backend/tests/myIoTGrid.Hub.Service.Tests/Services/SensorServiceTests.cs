using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class SensorServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly SensorService _sut;
    private readonly Mock<ILogger<SensorService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Guid _hubId = Guid.NewGuid();

    public SensorServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<SensorService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        var unitOfWork = new UnitOfWork(_context);

        // Create a Hub for Sensors
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = _hubId,
            TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        _sut = new SensorService(_context, unitOfWork, _signalRMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByHubAsync_WhenNoSensors_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetByHubAsync(_hubId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByHubAsync_ReturnsOnlyHubsSensors()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            SensorId = "sensor-1",
            Name = "Sensor 1",
            CreatedAt = DateTime.UtcNow
        });
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = Guid.NewGuid(), // Different Hub
            SensorId = "sensor-2",
            Name = "Sensor 2",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByHubAsync(_hubId);

        // Assert
        result.Should().ContainSingle();
        result.First().SensorId.Should().Be("sensor-1");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingSensor_ReturnsSensor()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            HubId = _hubId,
            SensorId = "test-sensor",
            Name = "Test Sensor",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(sensorId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Sensor");
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
    public async Task GetBySensorIdAsync_WithExistingSensor_ReturnsSensor()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            SensorId = "unique-sensor-id",
            Name = "Unique Sensor",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetBySensorIdAsync(_hubId, "unique-sensor-id");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unique Sensor");
    }

    [Fact]
    public async Task GetBySensorIdAsync_WithNonExistingSensor_ReturnsNull()
    {
        // Act
        var result = await _sut.GetBySensorIdAsync(_hubId, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateBySensorIdAsync_WithExistingSensor_UpdatesLastSeen()
    {
        // Arrange
        var existingSensor = new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            SensorId = "existing-sensor",
            Name = "Existing Sensor",
            LastSeen = DateTime.UtcNow.AddHours(-1),
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(existingSensor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetOrCreateBySensorIdAsync(_hubId, "existing-sensor");

        // Assert
        result.Should().NotBeNull();
        result.IsOnline.Should().BeTrue();
        result.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetOrCreateBySensorIdAsync_WithNewSensor_CreatesSensor()
    {
        // Act
        var result = await _sut.GetOrCreateBySensorIdAsync(_hubId, "new-sensor-id");

        // Assert
        result.Should().NotBeNull();
        result.SensorId.Should().Be("new-sensor-id");
        result.Name.Should().Be("New Sensor Id");
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSensor()
    {
        // Arrange
        var dto = new CreateSensorDto(
            HubId: _hubId,
            SensorId: "created-sensor",
            Name: "Created Sensor",
            Protocol: ProtocolDto.WLAN
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SensorId.Should().Be("created-sensor");
        result.Name.Should().Be("Created Sensor");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSensorId_ThrowsException()
    {
        // Arrange
        _context.Sensors.Add(new Sensor
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            SensorId = "duplicate-sensor",
            Name = "Duplicate",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateSensorDto(
            HubId: _hubId,
            SensorId: "duplicate-sensor",
            Name: "New Sensor"
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_WithoutHubId_ThrowsException()
    {
        // Arrange
        var dto = new CreateSensorDto(
            HubId: null,
            SensorId: "test-sensor",
            Name: "Test"
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HubId is required*");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingSensor_UpdatesSensor()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            HubId = _hubId,
            SensorId = "update-sensor",
            Name = "Original",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateSensorDto(Name: "Updated Name");

        // Act
        var result = await _sut.UpdateAsync(sensorId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
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
    public async Task UpdateLastSeenAsync_UpdatesLastSeenAndOnlineStatus()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            HubId = _hubId,
            SensorId = "lastseen-sensor",
            Name = "LastSeen Sensor",
            IsOnline = false,
            LastSeen = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.UpdateLastSeenAsync(sensorId);

        // Assert
        var sensor = await _sut.GetByIdAsync(sensorId);
        sensor!.IsOnline.Should().BeTrue();
        sensor.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateLastSeenAsync_WithNonExistingSensor_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.UpdateLastSeenAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetOnlineStatusAsync_SetsStatus()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            HubId = _hubId,
            SensorId = "online-sensor",
            Name = "Online Sensor",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetOnlineStatusAsync(sensorId, false);

        // Assert
        var sensor = await _sut.GetByIdAsync(sensorId);
        sensor!.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task SetOnlineStatusAsync_WithNonExistingSensor_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.SetOnlineStatusAsync(Guid.NewGuid(), false);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesSensorStatus()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            HubId = _hubId,
            SensorId = "status-sensor",
            Name = "Status Sensor",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var status = new SensorStatusDto(
            SensorId: sensorId,
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            BatteryLevel: 75
        );

        // Act
        await _sut.UpdateStatusAsync(sensorId, status);

        // Assert
        var sensor = await _sut.GetByIdAsync(sensorId);
        sensor.Should().NotBeNull();
        sensor!.IsOnline.Should().BeTrue();
        sensor.BatteryLevel.Should().Be(75);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNonExistingSensor_DoesNotThrow()
    {
        // Arrange
        var sensorId = Guid.NewGuid();
        var status = new SensorStatusDto(
            SensorId: sensorId,
            IsOnline: true,
            LastSeen: null,
            BatteryLevel: null
        );

        // Act & Assert
        var act = () => _sut.UpdateStatusAsync(sensorId, status);
        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("sensor-wohnzimmer-01", "Sensor Wohnzimmer 01")]
    [InlineData("temp_sensor_kitchen", "Temp Sensor Kitchen")]
    [InlineData("simple", "Simple")]
    [InlineData("", "Unknown Sensor")]
    public async Task GetOrCreateBySensorIdAsync_GeneratesCorrectName(string sensorId, string expectedName)
    {
        // Act
        var result = await _sut.GetOrCreateBySensorIdAsync(_hubId, sensorId);

        // Assert
        if (string.IsNullOrEmpty(sensorId))
        {
            result.Name.Should().Be("Unknown Sensor");
        }
        else
        {
            result.Name.Should().Be(expectedName);
        }
    }
}
