using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
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
    private readonly IMemoryCache _memoryCache;

    public SensorTypeServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<SensorTypeService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var unitOfWork = new UnitOfWork(_context);

        _sut = new SensorTypeService(_context, unitOfWork, _memoryCache, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _memoryCache.Dispose();
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
    public async Task GetAllAsync_WithSensorTypes_ReturnsAllOrderedByCategoryThenName()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "z_type",
            DisplayName = "Z Type",
            ClusterId = 0xFC00,
            Unit = "z",
            Category = "weather",
            CreatedAt = DateTime.UtcNow
        });
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "a_type",
            DisplayName = "A Type",
            ClusterId = 0xFC01,
            Unit = "a",
            Category = "air",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        // Ordered by Category first, then DisplayName
        result.First().TypeId.Should().Be("a_type"); // air comes before weather
        result.Last().TypeId.Should().Be("z_type");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsCorrectDtoProperties()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "temperature",
            DisplayName = "Temperatur",
            ClusterId = 0x0402,
            MatterClusterName = "TemperatureMeasurement",
            Unit = "°C",
            Resolution = 0.1,
            MinValue = -40,
            MaxValue = 125,
            Description = "Temperatur-Messung",
            IsCustom = false,
            Category = "weather",
            Icon = "thermostat",
            Color = "#FF5722",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).First();

        // Assert
        result.TypeId.Should().Be("temperature");
        result.DisplayName.Should().Be("Temperatur");
        result.ClusterId.Should().Be(0x0402u);
        result.MatterClusterName.Should().Be("TemperatureMeasurement");
        result.Unit.Should().Be("°C");
        result.Resolution.Should().Be(0.1);
        result.MinValue.Should().Be(-40);
        result.MaxValue.Should().Be(125);
        result.Description.Should().Be("Temperatur-Messung");
        result.IsCustom.Should().BeFalse();
        result.Category.Should().Be("weather");
        result.Icon.Should().Be("thermostat");
        result.Color.Should().Be("#FF5722");
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

    #region GetByTypeIdAsync Tests

    [Fact]
    public async Task GetByTypeIdAsync_WithExistingTypeId_ReturnsSensorType()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            TypeId: "humidity",
            DisplayName: "Humidity",
            ClusterId: 0x0405,
            Unit: "%"
        );
        await _sut.CreateAsync(dto);

        // Act
        var result = await _sut.GetByTypeIdAsync("humidity");

        // Assert
        result.Should().NotBeNull();
        result!.TypeId.Should().Be("humidity");
    }

    [Fact]
    public async Task GetByTypeIdAsync_WithNonExistingTypeId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByTypeIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTypeIdAsync_NormalizesToLowerCase()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "lower_case",
            DisplayName = "Lower Case Type",
            ClusterId = 0xFC00,
            Unit = "lc",
            Category = "other",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByTypeIdAsync("LOWER_CASE");

        // Assert
        result.Should().NotBeNull();
        result!.TypeId.Should().Be("lower_case");
    }

    [Fact]
    public async Task GetByTypeIdAsync_WithMixedCaseTypeId_FindsCorrectType()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "mixed_case",
            DisplayName = "Mixed Case",
            ClusterId = 0xFC00,
            Unit = "mc",
            Category = "other",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByTypeIdAsync("MiXeD_CaSe");

        // Assert
        result.Should().NotBeNull();
        result!.TypeId.Should().Be("mixed_case");
    }

    [Fact]
    public async Task GetByTypeIdAsync_WithCancellationToken_Works()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act
        var result = await _sut.GetByTypeIdAsync("test", cts.Token);

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
            TypeId: "temperature",
            DisplayName: "Temperature",
            ClusterId: 0x0402,
            Unit: "°C",
            MatterClusterName: "TemperatureMeasurement",
            Resolution: 0.1,
            MinValue: -40,
            MaxValue: 125,
            Description: "Temperature measurement",
            IsCustom: false,
            Category: "weather",
            Icon: "thermostat",
            Color: "#FF5722"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TypeId.Should().Be("temperature");
        result.DisplayName.Should().Be("Temperature");
        result.ClusterId.Should().Be(0x0402u);
        result.Unit.Should().Be("°C");
        result.MatterClusterName.Should().Be("TemperatureMeasurement");
        result.Description.Should().Be("Temperature measurement");
        result.Icon.Should().Be("thermostat");
    }

    [Fact]
    public async Task CreateAsync_NormalizesTypeIdToLowerCase()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            TypeId: "UPPER_CASE",
            DisplayName: "Upper Case",
            ClusterId: 0xFC00,
            Unit: "uc"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.TypeId.Should().Be("upper_case");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTypeId_ThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            TypeId: "temperature",
            DisplayName: "Temperature",
            ClusterId: 0x0402,
            Unit: "°C"
        );

        await _sut.CreateAsync(dto);

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateTypeIdDifferentCase_ThrowsException()
    {
        // Arrange
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "duplicate",
            DisplayName = "Duplicate",
            ClusterId = 0xFC00,
            Unit = "d",
            Category = "other",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateSensorTypeDto(
            TypeId: "DUPLICATE",
            DisplayName: "Another Duplicate",
            ClusterId: 0xFC01,
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
            TypeId: "minimal",
            DisplayName: "Minimal",
            ClusterId: 0xFC00,
            Unit: "m"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.TypeId.Should().Be("minimal");
        result.Description.Should().BeNull();
        result.Icon.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_SetsIsGlobalToFalse()
    {
        // Arrange
        var dto = new CreateSensorTypeDto(
            TypeId: "custom",
            DisplayName: "Custom",
            ClusterId: 0xFC00,
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
            TypeId: "test",
            DisplayName: "Test",
            ClusterId: 0xFC00,
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
        allTypes.Should().Contain(t => t.TypeId == "temperature");
        allTypes.Should().Contain(t => t.TypeId == "humidity");
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WhenCalledTwice_DoesNotDuplicate()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        var temperatureCount = allTypes.Count(t => t.TypeId == "temperature");
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

        allTypes.Count().Should().Be(defaultTypes.Count);
    }

    [Fact]
    public async Task SeedDefaultTypesAsync_WithExistingType_DoesNotDuplicate()
    {
        // Arrange - add one default type manually
        _context.SensorTypes.Add(new SensorType
        {
            TypeId = "temperature",
            DisplayName = "Existing Temperature",
            ClusterId = 0x0402,
            Unit = "°C",
            Category = "weather",
            IsGlobal = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var allTypes = await _sut.GetAllAsync();
        allTypes.Count(st => st.TypeId == "temperature").Should().Be(1);
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
    public async Task SeedDefaultTypesAsync_CreatesExpectedTypes(string typeId, string displayName, string unit)
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var sensorType = await _sut.GetByTypeIdAsync(typeId);
        sensorType.Should().NotBeNull();
        sensorType!.DisplayName.Should().Be(displayName);
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
    public async Task SeedDefaultTypesAsync_SetsCorrectMatterClusterIds()
    {
        // Act
        await _sut.SeedDefaultTypesAsync();

        // Assert
        var temperature = await _sut.GetByTypeIdAsync("temperature");
        var humidity = await _sut.GetByTypeIdAsync("humidity");
        var pressure = await _sut.GetByTypeIdAsync("pressure");

        temperature!.ClusterId.Should().Be(0x0402u);  // TemperatureMeasurement
        humidity!.ClusterId.Should().Be(0x0405u);    // RelativeHumidityMeasurement
        pressure!.ClusterId.Should().Be(0x0403u);    // PressureMeasurement
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

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ReturnsCorrectTypes()
    {
        // Arrange
        await _sut.SeedDefaultTypesAsync();

        // Act
        var weatherTypes = await _sut.GetByCategoryAsync("weather");

        // Assert
        weatherTypes.Should().NotBeEmpty();
        weatherTypes.Should().Contain(t => t.TypeId == "temperature");
        weatherTypes.Should().Contain(t => t.TypeId == "humidity");
        weatherTypes.Should().AllSatisfy(t => t.Category.Should().Be("weather"));
    }

    [Fact]
    public async Task GetByCategoryAsync_WithNonExistingCategory_ReturnsEmpty()
    {
        // Arrange
        await _sut.SeedDefaultTypesAsync();

        // Act
        var result = await _sut.GetByCategoryAsync("nonexistent");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetUnitAsync Tests

    [Fact]
    public async Task GetUnitAsync_WithExistingType_ReturnsUnit()
    {
        // Arrange
        await _sut.SeedDefaultTypesAsync();

        // Act
        var unit = await _sut.GetUnitAsync("temperature");

        // Assert
        unit.Should().Be("°C");
    }

    [Fact]
    public async Task GetUnitAsync_WithNonExistingType_ReturnsEmptyString()
    {
        // Act
        var unit = await _sut.GetUnitAsync("nonexistent");

        // Assert
        unit.Should().BeEmpty();
    }

    #endregion
}
