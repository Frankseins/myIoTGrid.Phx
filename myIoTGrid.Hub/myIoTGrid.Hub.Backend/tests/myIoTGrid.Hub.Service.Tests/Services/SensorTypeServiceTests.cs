using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Service.Tests.Services;

public class SensorTypeServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly SensorTypeService _sut;
    private readonly Mock<ILogger<SensorTypeService>> _loggerMock;

    public SensorTypeServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<SensorTypeService>>();
        var unitOfWork = new UnitOfWork(_context);

        _sut = new SensorTypeService(_context, unitOfWork, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_WhenNoSensorTypes_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSensorType()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            Code: "temperature",
            Name: "Temperature",
            Unit: "°C",
            Description: "Temperature measurement",
            IconName: "thermostat"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("temperature");
        result.Name.Should().Be("Temperature");
        result.Unit.Should().Be("°C");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateCode_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            Code: "temperature",
            Name: "Temperature",
            Unit: "°C"
        );

        await _sut.CreateAsync(dto);

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bereits*");
    }

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ReturnsSensorType()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            Code: "humidity",
            Name: "Humidity",
            Unit: "%"
        );
        await _sut.CreateAsync(dto);

        // Act
        var result = await _sut.GetByCodeAsync("humidity");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("humidity");
    }

    [Fact]
    public async Task GetByCodeAsync_WithNonExistingCode_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByCodeAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_CreatesDefaultTypes()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        allTypes.Should().HaveCountGreaterThan(0);
        allTypes.Should().Contain(t => t.Code == "temperature");
        allTypes.Should().Contain(t => t.Code == "humidity");
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WhenCalledTwice_DoesNotDuplicate()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        var temperatureCount = allTypes.Count(t => t.Code == "temperature");
        temperatureCount.Should().Be(1);
    }
}
