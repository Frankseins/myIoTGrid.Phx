using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class SeedDataServiceTests
{
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ISensorTypeService> _sensorTypeServiceMock;
    private readonly Mock<IAlertTypeService> _alertTypeServiceMock;
    private readonly Mock<ILogger<SeedDataService>> _loggerMock;
    private readonly SeedDataService _sut;

    public SeedDataServiceTests()
    {
        _tenantServiceMock = new Mock<ITenantService>();
        _sensorTypeServiceMock = new Mock<ISensorTypeService>();
        _alertTypeServiceMock = new Mock<IAlertTypeService>();
        _loggerMock = new Mock<ILogger<SeedDataService>>();

        _sut = new SeedDataService(
            _tenantServiceMock.Object,
            _sensorTypeServiceMock.Object,
            _alertTypeServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task SeedAllAsync_CallsAllSeedMethods()
    {
        // Arrange
        _tenantServiceMock.Setup(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sensorTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _alertTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedAllAsync();

        // Assert
        _tenantServiceMock.Verify(x => x.EnsureDefaultTenantAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sensorTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _alertTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

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
    public async Task SeedSensorTypesAsync_CallsSeedDefaultTypesAsync()
    {
        // Arrange
        _sensorTypeServiceMock.Setup(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SeedSensorTypesAsync();

        // Assert
        _sensorTypeServiceMock.Verify(x => x.SeedDefaultTypesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

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
}
