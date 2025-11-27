using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.Constants;
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

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenNoSensorTypes_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithSensorTypes_ReturnsAllOrderedByName()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "z_type",
            Name = "Z Type",
            Unit = "z",
            CreatedAt = DateTime.UtcNow
        });
        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "a_type",
            Name = "A Type",
            Unit = "a",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("A Type");
        result.Last().Name.Should().Be("Z Type");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCorrectDtoProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.SensorTypes.Add(new SensorType
        {
            Id = id,
            Code = "temperature",
            Name = "Temperatur",
            Unit = "°C",
            Description = "Temperatur-Messung",
            IconName = "thermostat",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).First();

        // Assert
        result.Id.Should().Be(id);
        result.Code.Should().Be("temperature");
        result.Name.Should().Be("Temperatur");
        result.Unit.Should().Be("°C");
        result.Description.Should().Be("Temperatur-Messung");
        result.IconName.Should().Be("thermostat");
        result.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetAllAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingSensorType_ReturnsSensorType()
    {
        // Arrange
        var id = Guid.NewGuid();
        _context.SensorTypes.Add(new SensorType
        {
            Id = id,
            Code = "test_type",
            Name = "Test Type",
            Unit = "unit",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("test_type");
        result.Name.Should().Be("Test Type");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyGuid_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid(), cts.Token);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCodeAsync Tests

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
    public async Task GetByCodeAsync_NormalizesCodeToLowerCase()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "lower_case",
            Name = "Lower Case Type",
            Unit = "lc",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByCodeAsync("LOWER_CASE");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("lower_case");
    }

    [Fact]
    public async Task GetByCodeAsync_WithMixedCaseCode_FindsCorrectType()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "mixed_case",
            Name = "Mixed Case",
            Unit = "mc",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByCodeAsync("MiXeD_CaSe");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("mixed_case");
    }

    [Fact]
    public async Task GetByCodeAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetByCodeAsync("test", cts.Token);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

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
        result.Description.Should().Be("Temperature measurement");
        result.IconName.Should().Be("thermostat");
    }

    [Fact]
    public async Task CreateAsync_NormalizesCodeToLowerCase()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            Code: "UPPER_CASE",
            Name: "Upper Case",
            Unit: "uc"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Code.Should().Be("upper_case");
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
    public async Task CreateAsync_WithDuplicateCodeDifferentCase_ThrowsException()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "duplicate",
            Name = "Duplicate",
            Unit = "d",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateSensorTypeDto(
            Code: "DUPLICATE",
            Name: "Another Duplicate",
            Unit: "d2"
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_WithMinimalDto_CreatesSensorType()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            Code: "minimal",
            Name: "Minimal",
            Unit: "m"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("minimal");
        result.Description.Should().BeNull();
        result.IconName.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsIsGlobalToFalse()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            Code: "custom",
            Name: "Custom",
            Unit: "c"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var dto = new CreateSensorTypeDto(
            Code: "test",
            Name: "Test",
            Unit: "t"
        );

        // Act
        var result = await _sut.CreateAsync(dto, cts.Token);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region SyncFromCloudAsync Tests

    [Fact]
    public async Task SyncFromCloudAsync_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.SyncFromCloudAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SyncFromCloudAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var act = () => _sut.SyncFromCloudAsync(cts.Token);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SeedDefaultTypesAsync Tests

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

    [Fact]
    public async Task SeedDefaultTypesAsync_WithEmptyDatabase_CreatesAllDefaultTypes()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        var defaultTypes = DefaultSensorTypes.GetAll();

        allTypes.Count().Should().Be(defaultTypes.Count());
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WithExistingType_DoesNotDuplicate()
    {
        // Arrange - add one default type manually
        _context.SensorTypes.Add(new SensorType
        {
            Id = Guid.NewGuid(),
            Code = "temperature",
            Name = "Existing Temperature",
            Unit = "°C",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        allTypes.Count(st => st.Code == "temperature").Should().Be(1);
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_SetsIsGlobalToTrue()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = _context.SensorTypes.ToList();
        allTypes.Should().AllSatisfy(st => st.IsGlobal.Should().BeTrue());
    }

    [Theory]
    [InlineData("temperature", "Temperatur", "°C")]
    [InlineData("humidity", "Luftfeuchtigkeit", "%")]
    [InlineData("pressure", "Luftdruck", "hPa")]
    [InlineData("co2", "CO2", "ppm")]
    [InlineData("pm25", "Feinstaub PM2.5", "µg/m³")]
    [InlineData("pm10", "Feinstaub PM10", "µg/m³")]
    [InlineData("battery", "Batterie", "%")]
    [InlineData("rssi", "Signalstärke", "dBm")]
    public async Task SeedDefaultTypesAsync_CreatesExpectedTypes(string code, string name, string unit)
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var sensorType = await _sut.GetByCodeAsync(code);
        sensorType.Should().NotBeNull();
        sensorType!.Name.Should().Be(name);
        sensorType.Unit.Should().Be(unit);
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_SetsCreatedAtToUtcNow()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var after = DateTime.UtcNow;
        var allTypes = _context.SensorTypes.ToList();
        allTypes.Should().AllSatisfy(st =>
        {
            st.CreatedAt.Should().BeOnOrAfter(before);
            st.CreatedAt.Should().BeOnOrBefore(after);
        });
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_GeneratesUniqueIds()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = _context.SensorTypes.ToList();
        var uniqueIds = allTypes.Select(st => st.Id).Distinct().ToList();
        uniqueIds.Count.Should().Be(allTypes.Count);
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var act = () => _sut.SeedDefaultTypesAsync(cts.Token);
        await act.Should().NotThrowAsync();
    }

    #endregion
}
