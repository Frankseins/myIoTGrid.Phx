using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for SensorService (v3.0 Two-Tier Model).
/// The Sensor entity now contains all configuration (previously split between SensorType and Sensor).
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
public class SensorServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly SensorService _sut;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ILogger<SensorService>> _loggerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public SensorServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _tenantServiceMock = new Mock<ITenantService>();
        _loggerMock = new Mock<ILogger<SensorService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var unitOfWork = new UnitOfWork(_context);

        _tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns(_tenantId);

        _sut = new SensorService(_context, unitOfWork, _tenantServiceMock.Object, _memoryCache, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _memoryCache.Dispose();
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenNoSensors_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSensorsForCurrentTenant()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Name.Should().Be("Test Sensor");
    }

    [Fact]
    public async Task GetAllAsync_DoesNotReturnOtherTenantSensors()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        var sensor = CreateTestSensor();
        sensor.TenantId = otherTenantId;
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        // Arrange
        _context.Sensors.Add(CreateTestSensor(name: "Zebra Sensor"));
        _context.Sensors.Add(CreateTestSensor(name: "Alpha Sensor"));
        _context.Sensors.Add(CreateTestSensor(name: "Beta Sensor"));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Alpha Sensor");
        result[1].Name.Should().Be("Beta Sensor");
        result[2].Name.Should().Be("Zebra Sensor");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingSensor_ReturnsSensor()
    {
        // Arrange
        var sensor = CreateTestSensor(name: "Living Room DHT22");
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(sensor.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Living Room DHT22");
        result.Code.Should().StartWith("dht22-test");
        result.Category.Should().Be("climate");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingSensor_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCodeAsync Tests

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ReturnsSensor()
    {
        // Arrange - Create sensor with fixed code (no random suffix for this test)
        var fixedCode = "unique-sensor-code";
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = fixedCode,
            Name = "Test Sensor",
            Protocol = CommunicationProtocol.OneWire,
            Category = "climate",
            IntervalSeconds = 60,
            MinIntervalSeconds = 2,
            OffsetCorrection = 0,
            GainCorrection = 1.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByCodeAsync(fixedCode);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(fixedCode);
    }

    [Fact]
    public async Task GetByCodeAsync_WithNonExistingCode_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByCodeAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ReturnsSensorsOfCategory()
    {
        // Arrange
        _context.Sensors.Add(CreateTestSensor(name: "Climate 1", category: "climate"));
        _context.Sensors.Add(CreateTestSensor(name: "Climate 2", category: "climate"));
        _context.Sensors.Add(CreateTestSensor(name: "Water 1", category: "water"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByCategoryAsync("climate");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.Category == "climate");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesSensor()
    {
        // Arrange - v3.0: CreateSensorDto has Code, Name, Protocol, Category
        var dto = new CreateSensorDto(
            Code: "kitchen-sensor",
            Name: "Kitchen Temperature Sensor",
            Protocol: CommunicationProtocolDto.OneWire,
            Category: "climate",
            Description: "Measures temperature in the kitchen",
            SerialNumber: "DHT22-001",
            IntervalSeconds: 30
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("kitchen-sensor");
        result.Name.Should().Be("Kitchen Temperature Sensor");
        result.Description.Should().Be("Measures temperature in the kitchen");
        result.SerialNumber.Should().Be("DHT22-001");
        result.IntervalSeconds.Should().Be(30);
        result.Category.Should().Be("climate");
        result.IsActive.Should().BeTrue();
        result.OffsetCorrection.Should().Be(0);
        result.GainCorrection.Should().Be(1.0);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalDto_CreatesSensorWithDefaults()
    {
        // Arrange - v3.0: Only Code, Name, Protocol, Category required
        var dto = new CreateSensorDto(
            Code: "simple-sensor",
            Name: "Simple Sensor",
            Protocol: CommunicationProtocolDto.Analog,
            Category: "custom"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("simple-sensor");
        result.Name.Should().Be("Simple Sensor");
        result.Description.Should().BeNull();
        result.SerialNumber.Should().BeNull();
        result.IntervalSeconds.Should().Be(60); // Default
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_SetsCorrectTenantId()
    {
        // Arrange
        var dto = new CreateSensorDto(
            Code: "tenant-sensor",
            Name: "Tenant Sensor",
            Protocol: CommunicationProtocolDto.I2C,
            Category: "climate"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public async Task CreateAsync_WithCapabilities_CreatesCapabilities()
    {
        // Arrange
        var capabilities = new[]
        {
            new CreateSensorCapabilityDto("temperature", "Temperature", "°C"),
            new CreateSensorCapabilityDto("humidity", "Humidity", "%")
        };

        var dto = new CreateSensorDto(
            Code: "multi-sensor",
            Name: "Multi Sensor",
            Protocol: CommunicationProtocolDto.I2C,
            Category: "climate",
            Capabilities: capabilities
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Capabilities.Should().HaveCount(2);
        result.Capabilities.Should().Contain(c => c.MeasurementType == "temperature");
        result.Capabilities.Should().Contain(c => c.MeasurementType == "humidity");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingSensor_UpdatesSensor()
    {
        // Arrange
        var sensor = CreateTestSensor(name: "Original Name");
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var dto = new UpdateSensorDto(
            Name: "Updated Name",
            Description: "New description",
            SerialNumber: "NEW-001",
            IsActive: false
        );

        // Act
        var result = await _sut.UpdateAsync(sensor.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("New description");
        result.SerialNumber.Should().Be("NEW-001");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_WithPartialDto_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var sensor = CreateTestSensor(name: "Original Name");
        sensor.Description = "Original description";
        sensor.SerialNumber = "ORIG-001";
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var dto = new UpdateSensorDto(Name: "Updated Name");

        // Act
        var result = await _sut.UpdateAsync(sensor.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Original description"); // Unchanged
        result.SerialNumber.Should().Be("ORIG-001"); // Unchanged
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingSensor_ThrowsException()
    {
        // Arrange
        var dto = new UpdateSensorDto(Name: "Test");

        // Act & Assert
        var act = () => _sut.UpdateAsync(Guid.NewGuid(), dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateAsync_WithIntervalSeconds_UpdatesInterval()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var dto = new UpdateSensorDto(IntervalSeconds: 120);

        // Act
        var result = await _sut.UpdateAsync(sensor.Id, dto);

        // Assert
        result.IntervalSeconds.Should().Be(120);
    }

    #endregion

    #region CalibrateAsync Tests

    [Fact]
    public async Task CalibrateAsync_WithValidDto_CalibratesSensor()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0.5,
            GainCorrection: 1.02,
            CalibrationNotes: "Calibrated with reference thermometer"
        );

        // Act
        var result = await _sut.CalibrateAsync(sensor.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result.OffsetCorrection.Should().Be(0.5);
        result.GainCorrection.Should().Be(1.02);
        result.CalibrationNotes.Should().Be("Calibrated with reference thermometer");
        result.LastCalibratedAt.Should().NotBeNull();
        result.LastCalibratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CalibrateAsync_WithCalibrationDueDate_SetsDueDate()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var dueDate = DateTime.UtcNow.AddMonths(6);
        var dto = new CalibrateSensorDto(
            OffsetCorrection: 0,
            GainCorrection: 1.0,
            CalibrationDueAt: dueDate
        );

        // Act
        var result = await _sut.CalibrateAsync(sensor.Id, dto);

        // Assert
        result.CalibrationDueAt.Should().BeCloseTo(dueDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CalibrateAsync_WithNonExistingSensor_ThrowsException()
    {
        // Arrange
        var dto = new CalibrateSensorDto(OffsetCorrection: 0, GainCorrection: 1.0);

        // Act & Assert
        var act = () => _sut.CalibrateAsync(Guid.NewGuid(), dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CalibrateAsync_UpdatesLastCalibratedAt()
    {
        // Arrange
        var sensor = CreateTestSensor();
        sensor.LastCalibratedAt = DateTime.UtcNow.AddYears(-1);
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        var dto = new CalibrateSensorDto(OffsetCorrection: 0.1, GainCorrection: 1.0);

        // Act
        var result = await _sut.CalibrateAsync(sensor.Id, dto);

        // Assert
        result.LastCalibratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingSensor_DeletesSensor()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(sensor.Id);

        // Assert
        var deleted = await _sut.GetByIdAsync(sensor.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingSensor_ThrowsException()
    {
        // Act & Assert
        var act = () => _sut.DeleteAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteAsync_WithActiveAssignments_ThrowsException()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);

        // Create a Hub and Node
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });

        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = hubId,
            NodeId = "test-node",
            Name = "Test Node",
            CreatedAt = DateTime.UtcNow
        });

        // Create an assignment
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            SensorId = sensor.Id,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act & Assert
        var act = () => _sut.DeleteAsync(sensor.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assignments*");
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.Sensors.Add(CreateTestSensor(name: $"Sensor {i:D2}", code: $"sensor-{i:D2}"));
        }
        await _context.SaveChangesAsync();

        // Page is 0-based: Page 0 = first page
        var queryParams = new QueryParamsDto { Page = 0, Size = 10 };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalRecords.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.Sensors.Add(CreateTestSensor(name: $"Sensor {i:D2}", code: $"sensor-{i:D2}"));
        }
        await _context.SaveChangesAsync();

        // Page is 0-based: Page 1 = second page
        var queryParams = new QueryParamsDto { Page = 1, Size = 10 };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetPagedAsync_SearchByName_FiltersResults()
    {
        // Arrange
        _context.Sensors.Add(CreateTestSensor(name: "Temperature Sensor", code: "temp"));
        _context.Sensors.Add(CreateTestSensor(name: "Humidity Sensor", code: "humidity"));
        _context.Sensors.Add(CreateTestSensor(name: "Pressure Sensor", code: "pressure"));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "Temperature" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Contain("Temperature");
    }

    [Fact]
    public async Task GetPagedAsync_SearchByCode_FiltersResults()
    {
        // Arrange
        _context.Sensors.Add(CreateTestSensor(name: "Sensor 1", code: "dht22"));
        _context.Sensors.Add(CreateTestSensor(name: "Sensor 2", code: "bme280"));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "bme" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Contain("bme280");
    }

    [Fact]
    public async Task GetPagedAsync_SearchByDescription_FiltersResults()
    {
        // Arrange
        var sensor1 = CreateTestSensor(name: "Sensor 1", code: "s1");
        sensor1.Description = "Measures temperature in kitchen";
        var sensor2 = CreateTestSensor(name: "Sensor 2", code: "s2");
        sensor2.Description = "Outdoor sensor";
        _context.Sensors.AddRange(sensor1, sensor2);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "kitchen" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Description.Should().Contain("kitchen");
    }

    [Fact]
    public async Task GetPagedAsync_SearchBySerialNumber_FiltersResults()
    {
        // Arrange
        var sensor1 = CreateTestSensor(name: "Sensor 1", code: "s1");
        sensor1.SerialNumber = "SN-12345";
        var sensor2 = CreateTestSensor(name: "Sensor 2", code: "s2");
        sensor2.SerialNumber = "SN-67890";
        _context.Sensors.AddRange(sensor1, sensor2);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "12345" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().SerialNumber.Should().Contain("12345");
    }

    [Fact]
    public async Task GetPagedAsync_SearchByManufacturer_FiltersResults()
    {
        // Arrange
        var sensor1 = CreateTestSensor(name: "Sensor 1", code: "s1");
        sensor1.Manufacturer = "Bosch";
        var sensor2 = CreateTestSensor(name: "Sensor 2", code: "s2");
        sensor2.Manufacturer = "Sensirion";
        _context.Sensors.AddRange(sensor1, sensor2);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "Bosch" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Manufacturer.Should().Be("Bosch");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByCategory_FiltersResults()
    {
        // Arrange
        _context.Sensors.Add(CreateTestSensor(name: "Climate Sensor", code: "climate1", category: "climate"));
        _context.Sensors.Add(CreateTestSensor(name: "Water Sensor", code: "water1", category: "water"));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "category", "water" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Category.Should().Be("water");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByProtocol_FiltersResults()
    {
        // Arrange
        var sensor1 = CreateTestSensor(name: "I2C Sensor", code: "i2c1");
        var sensor2 = CreateTestSensor(name: "OneWire Sensor", code: "ow1");
        sensor2.Protocol = CommunicationProtocol.I2C;
        _context.Sensors.AddRange(sensor1, sensor2);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "protocol", "I2C" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Code.Should().Contain("ow1");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByManufacturer_FiltersResults()
    {
        // Arrange
        var sensor1 = CreateTestSensor(name: "Sensor 1", code: "s1");
        sensor1.Manufacturer = "Bosch";
        var sensor2 = CreateTestSensor(name: "Sensor 2", code: "s2");
        sensor2.Manufacturer = "Sensirion";
        _context.Sensors.AddRange(sensor1, sensor2);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "manufacturer", "bosch" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Manufacturer.Should().Be("Bosch");
    }

    [Fact]
    public async Task GetPagedAsync_FilterByIsActive_FiltersResults()
    {
        // Arrange
        var sensor1 = CreateTestSensor(name: "Active Sensor", code: "active");
        sensor1.IsActive = true;
        var sensor2 = CreateTestSensor(name: "Inactive Sensor", code: "inactive");
        sensor2.IsActive = false;
        _context.Sensors.AddRange(sensor1, sensor2);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "isActive", "false" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().IsActive.Should().BeFalse();
    }

    #endregion

    #region GetCapabilitiesAsync Tests

    [Fact]
    public async Task GetCapabilitiesAsync_ReturnsCapabilitiesForSensor()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);

        var cap1 = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            MeasurementType = "temperature",
            DisplayName = "Temperature",
            Unit = "°C",
            SortOrder = 1,
            IsActive = true
        };
        var cap2 = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            MeasurementType = "humidity",
            DisplayName = "Humidity",
            Unit = "%",
            SortOrder = 0,
            IsActive = true
        };
        _context.SensorCapabilities.AddRange(cap1, cap2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetCapabilitiesAsync(sensor.Id)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].MeasurementType.Should().Be("humidity"); // SortOrder 0
        result[1].MeasurementType.Should().Be("temperature"); // SortOrder 1
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ExcludesInactiveCapabilities()
    {
        // Arrange
        var sensor = CreateTestSensor();
        _context.Sensors.Add(sensor);

        var cap1 = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            MeasurementType = "temperature",
            DisplayName = "Temperature",
            Unit = "°C",
            IsActive = true
        };
        var cap2 = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            MeasurementType = "humidity",
            DisplayName = "Humidity",
            Unit = "%",
            IsActive = false
        };
        _context.SensorCapabilities.AddRange(cap1, cap2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetCapabilitiesAsync(sensor.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public async Task GetCapabilitiesAsync_ReturnsEmptyForNonExistingSensor()
    {
        // Act
        var result = await _sut.GetCapabilitiesAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SeedDefaultSensorsAsync Tests

    [Fact]
    public async Task SeedDefaultSensorsAsync_CreatesDefaultSensors()
    {
        // Act
        await _sut.SeedDefaultSensorsAsync();

        // Assert
        var sensors = await _sut.GetAllAsync();
        sensors.Should().NotBeEmpty();
        sensors.Should().Contain(s => s.Code == "dht22");
        sensors.Should().Contain(s => s.Code == "bme280");
        sensors.Should().Contain(s => s.Code == "bh1750");
        sensors.Should().Contain(s => s.Code == "ds18b20");
    }

    [Fact]
    public async Task SeedDefaultSensorsAsync_DoesNotDuplicateExisting()
    {
        // Arrange - Add existing sensor with same code
        var existingSensor = new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = "dht22",
            Name = "Existing DHT22",
            Protocol = CommunicationProtocol.Digital,
            Category = "climate",
            IntervalSeconds = 60,
            MinIntervalSeconds = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(existingSensor);
        await _context.SaveChangesAsync();

        // Act
        await _sut.SeedDefaultSensorsAsync();

        // Assert
        var dht22Sensors = (await _sut.GetAllAsync()).Where(s => s.Code == "dht22").ToList();
        dht22Sensors.Should().HaveCount(1);
        dht22Sensors.First().Name.Should().Be("Existing DHT22"); // Original name preserved
    }

    [Fact]
    public async Task SeedDefaultSensorsAsync_CreatesCapabilitiesForNewSensors()
    {
        // Act
        await _sut.SeedDefaultSensorsAsync();

        // Assert
        var dht22 = (await _sut.GetAllAsync()).FirstOrDefault(s => s.Code == "dht22");
        dht22.Should().NotBeNull();
        dht22!.Capabilities.Should().NotBeEmpty();
        dht22.Capabilities.Should().Contain(c => c.MeasurementType == "temperature");
        dht22.Capabilities.Should().Contain(c => c.MeasurementType == "humidity");
    }

    [Fact]
    public async Task SeedDefaultSensorsAsync_SetsCorrectProtocols()
    {
        // Act
        await _sut.SeedDefaultSensorsAsync();

        // Assert
        var sensors = (await _sut.GetAllAsync()).ToList();

        var dht22 = sensors.FirstOrDefault(s => s.Code == "dht22");
        dht22?.Protocol.Should().Be(CommunicationProtocolDto.Digital);

        var bme280 = sensors.FirstOrDefault(s => s.Code == "bme280");
        bme280?.Protocol.Should().Be(CommunicationProtocolDto.I2C);

        var ds18b20 = sensors.FirstOrDefault(s => s.Code == "ds18b20");
        ds18b20?.Protocol.Should().Be(CommunicationProtocolDto.OneWire);
    }

    [Fact]
    public async Task SeedDefaultSensorsAsync_SetsCorrectCategories()
    {
        // Act
        await _sut.SeedDefaultSensorsAsync();

        // Assert
        var sensors = (await _sut.GetAllAsync()).ToList();

        var climateSensors = sensors.Where(s => s.Category == "climate").ToList();
        climateSensors.Should().Contain(s => s.Code == "dht22");
        climateSensors.Should().Contain(s => s.Code == "bme280");
        climateSensors.Should().Contain(s => s.Code == "bh1750");

        var waterSensors = sensors.Where(s => s.Category == "water").ToList();
        waterSensors.Should().Contain(s => s.Code == "ds18b20");
    }

    #endregion

    #region Helper Methods

    private Sensor CreateTestSensor(
        string name = "Test Sensor",
        string code = "dht22-test",
        string category = "climate")
    {
        return new Sensor
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            Code = code + "-" + Guid.NewGuid().ToString("N")[..8], // Ensure unique code
            Name = name,
            Protocol = CommunicationProtocol.OneWire,
            Category = category,
            IntervalSeconds = 60,
            MinIntervalSeconds = 2,
            OffsetCorrection = 0,
            GainCorrection = 1.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
