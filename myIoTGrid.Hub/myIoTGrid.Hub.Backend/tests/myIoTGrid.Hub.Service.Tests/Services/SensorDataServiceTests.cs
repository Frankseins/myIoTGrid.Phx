using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class SensorDataServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly SensorDataService _sut;
    private readonly Mock<ILogger<SensorDataService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Mock<IMatterBridgeClient> _matterBridgeMock;
    private readonly ITenantService _tenantService;
    private readonly IHubService _hubService;
    private readonly ISensorService _sensorService;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId;
    private readonly Guid _sensorId;
    private readonly Guid _sensorTypeId;

    public SensorDataServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<SensorDataService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        _matterBridgeMock = new Mock<IMatterBridgeClient>();
        var unitOfWork = new UnitOfWork(_context);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Hub:DefaultTenantId", _tenantId.ToString() }
            })
            .Build();

        _tenantService = new TenantService(
            _context, unitOfWork, Mock.Of<ILogger<TenantService>>(), config);

        _hubService = new HubService(
            _context, unitOfWork, _tenantService, _signalRMock.Object, Mock.Of<ILogger<HubService>>());

        _sensorService = new SensorService(
            _context, unitOfWork, _signalRMock.Object, Mock.Of<ILogger<SensorService>>());

        // Setup Matter Bridge Mock
        _matterBridgeMock.Setup(m => m.UpdateDeviceValueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create Hub and Sensor for tests
        _hubId = Guid.NewGuid();
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = _hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });

        _sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = _sensorId,
            HubId = _hubId,
            SensorId = "test-sensor",
            Name = "Test Sensor",
            CreatedAt = DateTime.UtcNow
        });

        // Create SensorTypes
        _sensorTypeId = Guid.NewGuid();
        _context.SensorTypes.Add(new SensorType
        {
            Id = _sensorTypeId,
            Code = "temperature",
            Name = "Temperatur",
            Unit = "°C",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "humidity",
            Name = "Luftfeuchtigkeit",
            Unit = "%",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();

        _sut = new SensorDataService(
            _context, unitOfWork, _tenantService, _hubService, _sensorService,
            _signalRMock.Object, _matterBridgeMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSensorData()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "test-sensor",
            SensorType: "temperature",
            Value: 21.5,
            HubId: "test-hub"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.SensorTypeCode.Should().Be("temperature");
        result.Value.Should().Be(21.5);
        result.Unit.Should().Be("°C");
        result.IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithNewSensor_AutoCreatesSensor()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "new-auto-sensor",
            SensorType: "temperature",
            Value: 22.0,
            HubId: "test-hub"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();

        // Verify sensor was created
        var sensor = await _sensorService.GetBySensorIdAsync(_hubId, "new-auto-sensor");
        sensor.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithNewHub_AutoCreatesHub()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-new-hub",
            SensorType: "temperature",
            Value: 23.0,
            HubId: "auto-created-hub"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();

        // Verify hub was created
        var hub = await _hubService.GetByHubIdAsync("auto-created-hub");
        hub.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithoutHubId_UsesDefaultHub()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "sensor-default",
            SensorType: "temperature",
            Value: 24.0
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_WithInvalidSensorType_ThrowsException()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "test-sensor",
            SensorType: "invalid_type",
            Value: 25.0
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateAsync_NotifiesSignalR()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "test-sensor",
            SensorType: "temperature",
            Value: 26.0,
            HubId: "test-hub"
        );

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _signalRMock.Verify(x => x.NotifyNewSensorDataAsync(
            _tenantId,
            It.IsAny<SensorDataDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingSensorData_ReturnsSensorData()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "test-sensor",
            SensorType: "temperature",
            Value: 27.0,
            HubId: "test-hub"
        );
        var created = await _sut.CreateAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Value.Should().Be(27.0);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingSensorData_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentTenant_ReturnsNull()
    {
        // Arrange
        var sensorDataId = Guid.NewGuid();
        _context.SensorData.Add(new SensorData
        {
            Id = sensorDataId,
            TenantId = Guid.NewGuid(), // Different tenant
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 28.0,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(sensorDataId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFilteredAsync_WithNoFilters_ReturnsAllTenantData()
    {
        // Arrange
        await CreateTestSensorDataAsync("temperature", 20.0);
        await CreateTestSensorDataAsync("humidity", 50.0);

        var filter = new SensorDataFilterDto();

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetFilteredAsync_WithSensorTypeFilter_FiltersCorrectly()
    {
        // Arrange
        await CreateTestSensorDataAsync("temperature", 21.0);
        await CreateTestSensorDataAsync("humidity", 55.0);

        var filter = new SensorDataFilterDto(SensorTypeCode: "temperature");

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().SensorTypeCode.Should().Be("temperature");
    }

    [Fact]
    public async Task GetFilteredAsync_WithDateRangeFilter_FiltersCorrectly()
    {
        // Arrange
        var oldData = new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 15.0,
            Timestamp = DateTime.UtcNow.AddDays(-10)
        };
        _context.SensorData.Add(oldData);

        await CreateTestSensorDataAsync("temperature", 22.0);
        await _context.SaveChangesAsync();

        var filter = new SensorDataFilterDto(
            From: DateTime.UtcNow.AddDays(-1),
            To: DateTime.UtcNow.AddDays(1)
        );

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Value.Should().Be(22.0);
    }

    [Fact]
    public async Task GetFilteredAsync_WithSyncStatusFilter_FiltersCorrectly()
    {
        // Arrange
        var syncedData = new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 23.0,
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = true
        };
        _context.SensorData.Add(syncedData);

        await CreateTestSensorDataAsync("temperature", 24.0);
        await _context.SaveChangesAsync();

        var filter = new SensorDataFilterDto(IsSyncedToCloud: false);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public async Task GetFilteredAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.SensorData.Add(new SensorData
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                SensorId = _sensorId,
                SensorTypeId = _sensorTypeId,
                Value = i,
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        var filter = new SensorDataFilterDto(Page: 2, PageSize: 5);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetFilteredAsync_OrdersByTimestampDescending()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            _context.SensorData.Add(new SensorData
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                SensorId = _sensorId,
                SensorTypeId = _sensorTypeId,
                Value = i,
                Timestamp = DateTime.UtcNow.AddMinutes(-i * 10)
            });
        }
        await _context.SaveChangesAsync();

        var filter = new SensorDataFilterDto();

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        var items = result.Items.ToList();
        for (int i = 0; i < items.Count - 1; i++)
        {
            items[i].Timestamp.Should().BeOnOrAfter(items[i + 1].Timestamp);
        }
    }

    [Fact]
    public async Task GetLatestByHubAsync_ReturnsLatestPerSensorType()
    {
        // Arrange
        // Add older temperature data
        _context.SensorData.Add(new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 18.0,
            Timestamp = DateTime.UtcNow.AddHours(-2)
        });

        // Add newer temperature data
        _context.SensorData.Add(new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 25.0,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetLatestByHubAsync(_sensorId)).ToList();

        // Assert
        result.Should().ContainSingle();
        result.First().Value.Should().Be(25.0); // Latest value
    }

    [Fact]
    public async Task GetLatestByHubAsync_WhenNoData_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetLatestByHubAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsLatestPerSensorAndType()
    {
        // Arrange
        await CreateTestSensorDataAsync("temperature", 30.0);
        await CreateTestSensorDataAsync("humidity", 60.0);

        // Act
        var result = (await _sut.GetLatestAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLatestAsync_WhenNoData_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetLatestAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by InMemory provider")]
    public async Task MarkAsSyncedAsync_MarksSpecifiedRecordsAsSynced()
    {
        // Arrange
        var data1 = new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 31.0,
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false
        };
        var data2 = new SensorData
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            SensorTypeId = _sensorTypeId,
            Value = 32.0,
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false
        };
        _context.SensorData.AddRange(data1, data2);
        await _context.SaveChangesAsync();

        // Act
        await _sut.MarkAsSyncedAsync([data1.Id, data2.Id]);

        // Assert - reload to verify
        _context.ChangeTracker.Clear();
        var filter = new SensorDataFilterDto(IsSyncedToCloud: true);
        var result = await _sut.GetFilteredAsync(filter);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarkAsSyncedAsync_WithEmptyList_DoesNothing()
    {
        // Act & Assert
        var act = () => _sut.MarkAsSyncedAsync([]);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFilteredAsync_WithSensorIdentifierFilter_FiltersCorrectly()
    {
        // Arrange
        await CreateTestSensorDataAsync("temperature", 33.0);

        var filter = new SensorDataFilterDto(SensorIdentifier: "test-sensor");

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_WithHubIdFilter_FiltersCorrectly()
    {
        // Arrange
        await CreateTestSensorDataAsync("temperature", 34.0);

        var filter = new SensorDataFilterDto(HubId: _hubId);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_WithSensorIdFilter_FiltersCorrectly()
    {
        // Arrange
        await CreateTestSensorDataAsync("temperature", 35.0);

        var filter = new SensorDataFilterDto(SensorId: _sensorId);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_UpdatesMatterBridge()
    {
        // Arrange
        var dto = new CreateSensorDataDto(
            SensorId: "test-sensor",
            SensorType: "temperature",
            Value: 36.0,
            HubId: "test-hub"
        );

        // Act
        await _sut.CreateAsync(dto);

        // Allow async fire-and-forget to complete
        await Task.Delay(100);

        // Assert
        _matterBridgeMock.Verify(m => m.UpdateDeviceValueAsync(
            It.IsAny<string>(),
            "temperature",
            36.0,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private async Task<SensorDataDto> CreateTestSensorDataAsync(string sensorType, double value)
    {
        var dto = new CreateSensorDataDto(
            SensorId: "test-sensor",
            SensorType: sensorType,
            Value: value,
            HubId: "test-hub"
        );
        return await _sut.CreateAsync(dto);
    }
}
