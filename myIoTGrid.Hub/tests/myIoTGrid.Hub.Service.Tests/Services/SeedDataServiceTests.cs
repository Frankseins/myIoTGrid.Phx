using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Unit tests for SeedDataService (v3.0).
/// Tests seeding of default data without SensorType (now handled by Sensor directly).
/// </summary>
public class SeedDataServiceTests
{
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IAlertTypeService> _alertTypeServiceMock;
    private readonly Mock<ISensorService> _sensorServiceMock;
    private readonly Mock<IHubService> _hubServiceMock;
    private readonly Mock<ILogger<SeedDataService>> _loggerMock;
    private readonly SeedDataService _sut;

    public SeedDataServiceTests()
    {
        _tenantServiceMock = new Mock<ITenantService>();
        _alertTypeServiceMock = new Mock<IAlertTypeService>();
        _sensorServiceMock = new Mock<ISensorService>();
        _hubServiceMock = new Mock<IHubService>();
        _loggerMock = new Mock<ILogger<SeedDataService>>();

        _sut = new SeedDataService(
            _tenantServiceMock.Object,
            _alertTypeServiceMock.Object,
            _sensorServiceMock.Object,
            _hubServiceMock.Object,
            _loggerMock.Object
        );
    }

    #region SeedAllAsync Tests

    [Fact]
    public async Task SeedAllAsync_CallsAllSeedMethods()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAllAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Once);
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()), Times.Once);
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAllAsync_CallsMethodsInCorrectOrder()
    {
        // Arrange
        var callOrder = new List<string>();

        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Tenant"))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Hub"))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("AlertType"))
            .Returns(Task.CompletedTask);
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Sensor"))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAllAsync();

        // Assert - v3.0: Order is Tenant, Hub, AlertType, Sensor (no SensorType)
        callOrder.Should().ContainInOrder("Tenant", "Hub", "AlertType", "Sensor");
    }

    [Fact]
    public async Task SeedAllAsync_WithCancellationToken_PassesTokenToAllMethods()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(token))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(token))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(token))
            .Returns(Task.CompletedTask);
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(token))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAllAsync(token);

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(token), Times.Once);
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(token), Times.Once);
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(token), Times.Once);
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(token), Times.Once);
    }

    [Fact]
    public async Task SeedAllAsync_WhenTenantServiceThrows_PropagatesException()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tenant error"));

        // Act & Assert
        var act = () => _sut.SeedAllAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Tenant error");
    }

    [Fact]
    public async Task SeedAllAsync_WhenHubServiceThrows_PropagatesException()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Hub error"));

        // Act & Assert
        var act = () => _sut.SeedAllAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Hub error");
    }

    [Fact]
    public async Task SeedAllAsync_WhenAlertTypeServiceThrows_PropagatesException()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AlertType error"));

        // Act & Assert
        var act = () => _sut.SeedAllAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AlertType error");
    }

    [Fact]
    public async Task SeedAllAsync_WhenSensorServiceThrows_PropagatesException()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sensor error"));

        // Act & Assert
        var act = () => _sut.SeedAllAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Sensor error");
    }

    #endregion

    #region SeedTenantAsync Tests

    [Fact]
    public async Task SeedTenantAsync_CallsEnsureDefaultTenantAsync()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedTenantAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedTenantAsync_WithCancellationToken_PassesToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(token))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedTenantAsync(token);

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(token), Times.Once);
    }

    [Fact]
    public async Task SeedTenantAsync_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act & Assert
        var act = () => _sut.SeedTenantAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedTenantAsync_DoesNotCallOtherServices()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedTenantAsync();

        // Assert
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()), Times.Never);
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region SeedHubAsync Tests

    [Fact]
    public async Task SeedHubAsync_CallsEnsureDefaultHubAsync()
    {
        // Arrange
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedHubAsync();

        // Assert
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedHubAsync_WithCancellationToken_PassesToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(token))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedHubAsync(token);

        // Assert
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(token), Times.Once);
    }

    [Fact]
    public async Task SeedHubAsync_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act & Assert
        var act = () => _sut.SeedHubAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedHubAsync_DoesNotCallOtherServices()
    {
        // Arrange
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedHubAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Never);
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region SeedAlertTypesAsync Tests

    [Fact]
    public async Task SeedAlertTypesAsync_CallsSeedDefaultTypesAsync()
    {
        // Arrange
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAlertTypesAsync();

        // Assert
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAlertTypesAsync_WithCancellationToken_PassesToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(token))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAlertTypesAsync(token);

        // Assert
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(token), Times.Once);
    }

    [Fact]
    public async Task SeedAlertTypesAsync_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act & Assert
        var act = () => _sut.SeedAlertTypesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedAlertTypesAsync_DoesNotCallOtherServices()
    {
        // Arrange
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAlertTypesAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Never);
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()), Times.Never);
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region SeedSensorsAsync Tests

    [Fact]
    public async Task SeedSensorsAsync_CallsSeedDefaultSensorsAsync()
    {
        // Arrange
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedSensorsAsync();

        // Assert
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedSensorsAsync_WithCancellationToken_PassesToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(token))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedSensorsAsync(token);

        // Assert
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(token), Times.Once);
    }

    [Fact]
    public async Task SeedSensorsAsync_WhenServiceThrows_PropagatesException()
    {
        // Arrange
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act & Assert
        var act = () => _sut.SeedSensorsAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedSensorsAsync_DoesNotCallOtherServices()
    {
        // Arrange
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedSensorsAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Never);
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()), Times.Never);
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public async Task SeedAllAsync_CalledMultipleTimes_CallsServicesMultipleTimes()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _hubServiceMock.Setup(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sensorServiceMock.Setup(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAllAsync();
        await _sut.SeedAllAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _hubServiceMock.Verify(x => x.EnsureDefaultHubAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _sensorServiceMock.Verify(x => x.SeedDefaultSensorsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion
}
