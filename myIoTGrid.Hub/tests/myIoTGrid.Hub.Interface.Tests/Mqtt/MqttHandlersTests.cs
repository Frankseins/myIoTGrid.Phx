using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Interface.Mqtt;

namespace myIoTGrid.Hub.Interface.Tests.Mqtt;

/// <summary>
/// Unit tests for ReadingMqttHandler.
/// </summary>
public class ReadingMqttHandlerTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IReadingService> _readingServiceMock;
    private readonly Mock<ILogger<ReadingMqttHandler>> _loggerMock;
    private readonly ReadingMqttHandler _sut;

    public ReadingMqttHandlerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _tenantServiceMock = new Mock<ITenantService>();
        _readingServiceMock = new Mock<IReadingService>();
        _loggerMock = new Mock<ILogger<ReadingMqttHandler>>();

        var scopedServiceProviderMock = new Mock<IServiceProvider>();
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ITenantService)))
            .Returns(_tenantServiceMock.Object);
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IReadingService)))
            .Returns(_readingServiceMock.Object);

        _scopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);

        _sut = new ReadingMqttHandler(_serviceProviderMock.Object, _loggerMock.Object);
    }

    #region CanHandle Tests

    [Theory]
    [InlineData("myiotgrid/00000000-0000-0000-0000-000000000001/readings", true)]
    [InlineData("myiotgrid/a1b2c3d4-e5f6-7890-abcd-ef1234567890/readings", true)]
    [InlineData("myiotgrid/invalid/readings", false)]
    [InlineData("myiotgrid/00000000-0000-0000-0000-000000000001/status", false)]
    [InlineData("other/00000000-0000-0000-0000-000000000001/readings", false)]
    [InlineData("", false)]
    public void CanHandle_ReturnsExpectedResult(string topic, bool expected)
    {
        // Act
        var result = _sut.CanHandle(topic);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region HandleMessageAsync Tests

    [Fact]
    public async Task HandleMessageAsync_ValidPayload_ProcessesReading()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/readings";
        var nodeId = "node-01";
        var payload = JsonSerializer.Serialize(new
        {
            NodeId = nodeId,
            EndpointId = 1,
            MeasurementType = "temperature",
            RawValue = 21.5
        });

        _readingServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateReadingDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadingDto(
                Id: 1,
                TenantId: tenantId,
                NodeId: Guid.NewGuid(),
                NodeName: "Test Node",
                AssignmentId: Guid.NewGuid(),
                SensorId: Guid.NewGuid(),
                SensorCode: "BME280",
                SensorName: "BME280 Sensor",
                SensorIcon: "thermostat",
                SensorColor: "#FF5722",
                MeasurementType: "temperature",
                DisplayName: "Temperature",
                RawValue: 21.5,
                Value: 21.5,
                Unit: "°C",
                Timestamp: DateTime.UtcNow,
                Location: null,
                IsSyncedToCloud: false
            ));

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeTrue();
        _tenantServiceMock.Verify(t => t.SetCurrentTenantId(tenantId), Times.Once);
        _readingServiceMock.Verify(r => r.CreateAsync(It.IsAny<CreateReadingDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidTopic_ReturnsFalse()
    {
        // Arrange
        var topic = "invalid/topic";
        var payload = "{}";

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/readings";
        var payload = "invalid json {{{";

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_MissingNodeId_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/readings";
        var payload = JsonSerializer.Serialize(new
        {
            EndpointId = 1,
            MeasurementType = "temperature",
            RawValue = 21.5
        });

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_MissingMeasurementType_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/readings";
        var payload = JsonSerializer.Serialize(new
        {
            NodeId = "node-01",
            EndpointId = 1,
            RawValue = 21.5
        });

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidEndpointId_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/readings";
        var payload = JsonSerializer.Serialize(new
        {
            NodeId = "node-01",
            EndpointId = 0,
            MeasurementType = "temperature",
            RawValue = 21.5
        });

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_ServiceThrows_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/readings";
        var payload = JsonSerializer.Serialize(new
        {
            NodeId = "node-01",
            EndpointId = 1,
            MeasurementType = "temperature",
            RawValue = 21.5
        });

        _readingServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateReadingDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

/// <summary>
/// Unit tests for HubStatusMqttHandler.
/// </summary>
public class HubStatusMqttHandlerTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IHubService> _hubServiceMock;
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly Mock<ILogger<HubStatusMqttHandler>> _loggerMock;
    private readonly HubStatusMqttHandler _sut;

    public HubStatusMqttHandlerTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeMock = new Mock<IServiceScope>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _tenantServiceMock = new Mock<ITenantService>();
        _hubServiceMock = new Mock<IHubService>();
        _alertServiceMock = new Mock<IAlertService>();
        _loggerMock = new Mock<ILogger<HubStatusMqttHandler>>();

        var scopedServiceProviderMock = new Mock<IServiceProvider>();
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(ITenantService)))
            .Returns(_tenantServiceMock.Object);
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IHubService)))
            .Returns(_hubServiceMock.Object);
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(IAlertService)))
            .Returns(_alertServiceMock.Object);

        _scopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);
        _scopeFactoryMock.Setup(f => f.CreateScope()).Returns(_scopeMock.Object);

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_scopeFactoryMock.Object);

        _sut = new HubStatusMqttHandler(_serviceProviderMock.Object, _loggerMock.Object);
    }

    #region CanHandle Tests

    [Theory]
    [InlineData("myiotgrid/00000000-0000-0000-0000-000000000001/hubs/hub-001/status", true)]
    [InlineData("myiotgrid/a1b2c3d4-e5f6-7890-abcd-ef1234567890/hubs/test-hub/status", true)]
    [InlineData("myiotgrid/invalid/hubs/hub-001/status", false)]
    [InlineData("myiotgrid/00000000-0000-0000-0000-000000000001/readings", false)]
    [InlineData("", false)]
    public void CanHandle_ReturnsExpectedResult(string topic, bool expected)
    {
        // Act
        var result = _sut.CanHandle(topic);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region HandleMessageAsync Tests

    [Fact]
    public async Task HandleMessageAsync_ValidOnlinePayload_UpdatesHubStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var hubId = "hub-001";
        var topic = $"myiotgrid/{tenantId}/hubs/{hubId}/status";
        var payload = "online";  // Status payload as simple string

        var hubGuid = Guid.NewGuid();
        _hubServiceMock.Setup(h => h.GetOrCreateByHubIdAsync(hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubDto(
                Id: hubGuid,
                TenantId: tenantId,
                HubId: hubId,
                Name: "Test Hub",
                Description: null,
                LastSeen: DateTime.UtcNow,
                IsOnline: true,
                CreatedAt: DateTime.UtcNow,
                SensorCount: 0
            ));

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeTrue();
        _tenantServiceMock.Verify(t => t.SetCurrentTenantId(tenantId), Times.Once);
        _hubServiceMock.Verify(h => h.SetOnlineStatusAsync(hubGuid, true, It.IsAny<CancellationToken>()), Times.Once);
        _alertServiceMock.Verify(a => a.DeactivateHubAlertsAsync(hubGuid, "hub_offline", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_ValidOfflinePayload_CreatesAlert()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var hubId = "hub-001";
        var topic = $"myiotgrid/{tenantId}/hubs/{hubId}/status";
        var payload = "offline";

        var hubGuid = Guid.NewGuid();
        _hubServiceMock.Setup(h => h.GetOrCreateByHubIdAsync(hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubDto(
                Id: hubGuid,
                TenantId: tenantId,
                HubId: hubId,
                Name: "Test Hub",
                Description: null,
                LastSeen: DateTime.UtcNow,
                IsOnline: false,
                CreatedAt: DateTime.UtcNow,
                SensorCount: 0
            ));

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeTrue();
        _hubServiceMock.Verify(h => h.SetOnlineStatusAsync(hubGuid, false, It.IsAny<CancellationToken>()), Times.Once);
        _alertServiceMock.Verify(a => a.CreateHubOfflineAlertAsync(hubGuid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidTopic_ReturnsFalse()
    {
        // Arrange
        var topic = "invalid/topic";
        var payload = "online";

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_InvalidPayload_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/hubs/hub-001/status";
        var payload = "invalid-status";

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleMessageAsync_ServiceThrows_ReturnsFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var topic = $"myiotgrid/{tenantId}/hubs/hub-001/status";
        var payload = "online";

        _hubServiceMock.Setup(h => h.GetOrCreateByHubIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("0", false)]
    [InlineData("false", false)]
    public async Task HandleMessageAsync_AlternativeStatusFormats_WorksCorrectly(string payload, bool expectedOnline)
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var hubId = "hub-001";
        var topic = $"myiotgrid/{tenantId}/hubs/{hubId}/status";

        var hubGuid = Guid.NewGuid();
        _hubServiceMock.Setup(h => h.GetOrCreateByHubIdAsync(hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubDto(
                Id: hubGuid,
                TenantId: tenantId,
                HubId: hubId,
                Name: "Test Hub",
                Description: null,
                LastSeen: DateTime.UtcNow,
                IsOnline: expectedOnline,
                CreatedAt: DateTime.UtcNow,
                SensorCount: 0
            ));

        // Act
        var result = await _sut.HandleMessageAsync(topic, payload);

        // Assert
        result.Should().BeTrue();
        _hubServiceMock.Verify(h => h.SetOnlineStatusAsync(hubGuid, expectedOnline, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
