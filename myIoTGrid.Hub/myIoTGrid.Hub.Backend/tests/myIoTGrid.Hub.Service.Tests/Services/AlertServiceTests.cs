using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Infrastructure.Matter;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class AlertServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly AlertService _sut;
    private readonly Mock<ILogger<AlertService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Mock<IMatterBridgeClient> _matterBridgeMock;
    private readonly ITenantService _tenantService;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId;
    private readonly Guid _sensorId;
    private readonly Guid _alertTypeId;

    public AlertServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<AlertService>>();
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

        // Setup Matter Bridge Mock (always succeeds)
        _matterBridgeMock.Setup(m => m.RegisterDeviceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _matterBridgeMock.Setup(m => m.SetContactSensorStateAsync(
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Create a Hub and Sensor for tests
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

        // Create AlertTypes
        _alertTypeId = Guid.NewGuid();
        _context.AlertTypes.Add(new AlertType
        {
            Id = _alertTypeId,
            Code = "mold_risk",
            Name = "Schimmelrisiko",
            DefaultLevel = AlertLevel.Warning,
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.AlertTypes.Add(new AlertType
        {
            Id = Guid.NewGuid(),
            Code = "hub_offline",
            Name = "Hub Offline",
            DefaultLevel = AlertLevel.Critical,
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();

        _sut = new AlertService(_context, unitOfWork, _tenantService, _signalRMock.Object, _matterBridgeMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateFromCloudAsync_WithValidDto_CreatesAlert()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            HubId: "test-hub",
            SensorId: "test-sensor",
            Level: AlertLevelDto.Warning,
            Message: "High humidity detected",
            Recommendation: "Open windows"
        );

        // Act
        var result = await _sut.CreateFromCloudAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.AlertTypeCode.Should().Be("mold_risk");
        result.Level.Should().Be(AlertLevelDto.Warning);
        result.Message.Should().Be("High humidity detected");
        result.Source.Should().Be(AlertSourceDto.Cloud);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateLocalAlertAsync_WithValidDto_CreatesLocalAlert()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Local warning"
        );

        // Act
        var result = await _sut.CreateLocalAlertAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Source.Should().Be(AlertSourceDto.Local);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateFromCloudAsync_WithInvalidAlertType_ThrowsException()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "invalid_type",
            Message: "Test"
        );

        // Act & Assert
        var act = () => _sut.CreateFromCloudAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateFromCloudAsync_NotifiesSignalR()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            Message: "Test alert"
        );

        // Act
        await _sut.CreateFromCloudAsync(dto);

        // Assert
        _signalRMock.Verify(x => x.NotifyAlertReceivedAsync(
            _tenantId,
            It.IsAny<AlertDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingAlert_ReturnsAlert()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Test");
        var created = await _sut.CreateFromCloudAsync(dto);

        // Act
        var result = await _sut.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingAlert_ReturnsNull()
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
        var alertId = Guid.NewGuid();
        _context.Alerts.Add(new Alert
        {
            Id = alertId,
            TenantId = Guid.NewGuid(), // Different tenant
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Test",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(alertId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAsync_WhenNoAlerts_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetActiveAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveAlerts()
    {
        // Arrange
        var dto1 = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Active alert");
        var active = await _sut.CreateFromCloudAsync(dto1);

        var dto2 = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "To acknowledge");
        var toAck = await _sut.CreateFromCloudAsync(dto2);
        await _sut.AcknowledgeAsync(toAck.Id);

        // Act
        var result = await _sut.GetActiveAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task GetActiveAsync_OrdersByCriticalFirst()
    {
        // Arrange - create critical and warning alerts
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Warning",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Critical,
            Source = AlertSource.Cloud,
            Message = "Critical",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetActiveAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First().Level.Should().Be(AlertLevelDto.Critical);
    }

    [Fact]
    public async Task GetFilteredAsync_WithHubFilter_FiltersCorrectly()
    {
        // Arrange
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = _hubId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Hub alert",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = Guid.NewGuid(), // Different hub
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Other hub alert",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var filter = new AlertFilterDto(HubId: _hubId);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().HubId.Should().Be(_hubId);
    }

    [Fact]
    public async Task GetFilteredAsync_WithLevelFilter_FiltersCorrectly()
    {
        // Arrange
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Critical,
            Source = AlertSource.Cloud,
            Message = "Critical",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Warning",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var filter = new AlertFilterDto(Level: AlertLevelDto.Critical);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Level.Should().Be(AlertLevelDto.Critical);
    }

    [Fact]
    public async Task GetFilteredAsync_WithSourceFilter_FiltersCorrectly()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Cloud alert");
        await _sut.CreateFromCloudAsync(dto);

        var dto2 = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Local alert");
        await _sut.CreateLocalAlertAsync(dto2);

        var filter = new AlertFilterDto(Source: AlertSourceDto.Local);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Source.Should().Be(AlertSourceDto.Local);
    }

    [Fact]
    public async Task GetFilteredAsync_WithDateRangeFilter_FiltersCorrectly()
    {
        // Arrange
        var yesterday = DateTime.UtcNow.AddDays(-1);
        var tomorrow = DateTime.UtcNow.AddDays(1);

        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Today",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Cloud,
            Message = "Old",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        });
        await _context.SaveChangesAsync();

        var filter = new AlertFilterDto(From: yesterday, To: tomorrow);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Message.Should().Be("Today");
    }

    [Fact]
    public async Task GetFilteredAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.Alerts.Add(new Alert
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantId,
                AlertTypeId = _alertTypeId,
                Level = AlertLevel.Warning,
                Source = AlertSource.Cloud,
                Message = $"Alert {i}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        var filter = new AlertFilterDto(Page: 2, PageSize: 5);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task AcknowledgeAsync_WithExistingAlert_SetsAcknowledgedAtAndDeactivates()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Test");
        var created = await _sut.CreateFromCloudAsync(dto);

        // Act
        var result = await _sut.AcknowledgeAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.AcknowledgedAt.Should().NotBeNull();
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task AcknowledgeAsync_WithNonExistingAlert_ReturnsNull()
    {
        // Act
        var result = await _sut.AcknowledgeAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AcknowledgeAsync_NotifiesSignalR()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Test");
        var created = await _sut.CreateFromCloudAsync(dto);

        // Reset mock to track only acknowledge call
        _signalRMock.Reset();

        // Act
        await _sut.AcknowledgeAsync(created.Id);

        // Assert
        _signalRMock.Verify(x => x.NotifyAlertAcknowledgedAsync(
            _tenantId,
            It.IsAny<AlertDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateHubOfflineAlertAsync_WithValidSensor_CreatesAlert()
    {
        // Act
        await _sut.CreateHubOfflineAlertAsync(_sensorId);

        // Assert
        var alerts = await _sut.GetActiveAsync();
        alerts.Should().ContainSingle(a => a.AlertTypeCode == "hub_offline");
    }

    [Fact]
    public async Task CreateHubOfflineAlertAsync_WithNonExistingSensor_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.CreateHubOfflineAlertAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CreateHubOfflineAlertAsync_WithExistingActiveAlert_DoesNotDuplicate()
    {
        // Arrange
        await _sut.CreateHubOfflineAlertAsync(_sensorId);

        // Act
        await _sut.CreateHubOfflineAlertAsync(_sensorId);

        // Assert
        var alerts = await _sut.GetActiveAsync();
        alerts.Count(a => a.AlertTypeCode == "hub_offline").Should().Be(1);
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by InMemory provider")]
    public async Task DeactivateAlertsAsync_DeactivatesMatchingAlerts()
    {
        // Arrange
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            SensorId = _sensorId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Local,
            Message = "Active alert",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeactivateAlertsAsync(_sensorId, "mold_risk");

        // Assert
        var alerts = await _sut.GetActiveAsync();
        alerts.Should().BeEmpty();
    }

    [Fact(Skip = "ExecuteUpdateAsync is not supported by InMemory provider")]
    public async Task DeactivateHubAlertsAsync_DeactivatesMatchingAlerts()
    {
        // Arrange
        _context.Alerts.Add(new Alert
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = _hubId,
            AlertTypeId = _alertTypeId,
            Level = AlertLevel.Warning,
            Source = AlertSource.Local,
            Message = "Hub alert",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeactivateHubAlertsAsync(_hubId, "mold_risk");

        // Assert
        var alerts = await _sut.GetActiveAsync();
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilteredAsync_WithIsActiveFilter_FiltersCorrectly()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Active");
        var active = await _sut.CreateFromCloudAsync(dto);

        var dto2 = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Inactive");
        var inactive = await _sut.CreateFromCloudAsync(dto2);
        await _sut.AcknowledgeAsync(inactive.Id);

        var filter = new AlertFilterDto(IsActive: true);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetFilteredAsync_WithIsAcknowledgedFilter_FiltersCorrectly()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Not acknowledged");
        await _sut.CreateFromCloudAsync(dto);

        var dto2 = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Acknowledged");
        var acked = await _sut.CreateFromCloudAsync(dto2);
        await _sut.AcknowledgeAsync(acked.Id);

        var filter = new AlertFilterDto(IsAcknowledged: true);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().AcknowledgedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFilteredAsync_WithAlertTypeCodeFilter_FiltersCorrectly()
    {
        // Arrange
        var dto = new CreateAlertDto(AlertTypeCode: "mold_risk", Message: "Mold");
        await _sut.CreateFromCloudAsync(dto);

        var dto2 = new CreateAlertDto(AlertTypeCode: "hub_offline", Message: "Offline");
        await _sut.CreateLocalAlertAsync(dto2);

        var filter = new AlertFilterDto(AlertTypeCode: "mold_risk");

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().AlertTypeCode.Should().Be("mold_risk");
    }

    [Fact]
    public async Task CreateFromCloudAsync_UpdatesMatterBridge()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            SensorId: "test-sensor",
            Message: "Test"
        );

        // Act
        await _sut.CreateFromCloudAsync(dto);

        // Allow async fire-and-forget to complete
        await Task.Delay(100);

        // Assert - verify Matter Bridge was called
        _matterBridgeMock.Verify(m => m.RegisterDeviceAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "contact",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _matterBridgeMock.Verify(m => m.SetContactSensorStateAsync(
            It.IsAny<string>(),
            true, // isOpen = true for active alert
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
