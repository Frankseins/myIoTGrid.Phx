using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Unit tests for DashboardService.
/// </summary>
public class DashboardServiceTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly DashboardService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _hubId = Guid.NewGuid();

    public DashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HubDbContext(options);
        _tenantServiceMock = new Mock<ITenantService>();
        _loggerMock = new Mock<ILogger<DashboardService>>();

        _tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns(_tenantId);

        _service = new DashboardService(_context, _tenantServiceMock.Object, _loggerMock.Object);

        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var tenant = new Tenant
        {
            Id = _tenantId,
            Name = "Test Tenant",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Tenants.Add(tenant);

        var hub = new HubEntity
        {
            Id = _hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Hubs.Add(hub);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetLocationDashboardAsync Tests

    [Fact]
    public async Task GetLocationDashboardAsync_WithNoNodes_ReturnsEmptyDashboard()
    {
        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WithNodeButNoReadings_ReturnsEmptyLocations()
    {
        // Arrange
        var node = new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "test-node-001",
            Name = "Test Node",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            Location = new Location("Wohnzimmer"),
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().BeEmpty(); // No readings = no widgets = location filtered out
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WithReadings_ReturnsLocationGroup()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().NotBeEmpty();
        result.Locations.First().LocationName.Should().Be("Wohnzimmer");
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WithHeroLocation_SetsIsHeroTrue()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Außen");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        var outsideLocation = result.Locations.FirstOrDefault(l => l.LocationName == "Außen");
        outsideLocation.Should().NotBeNull();
        outsideLocation!.IsHero.Should().BeTrue();
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WithNonHeroLocation_SetsIsHeroFalse()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Schlafzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Should().NotBeNull();
        var location = result.Locations.FirstOrDefault(l => l.LocationName == "Schlafzimmer");
        location.Should().NotBeNull();
        location!.IsHero.Should().BeFalse();
    }

    [Theory]
    [InlineData("außen", "home")]
    [InlineData("wohnzimmer", "weekend")]
    [InlineData("küche", "kitchen")]
    [InlineData("badezimmer", "bathroom")]
    [InlineData("garten", "yard")]
    [InlineData("garage", "garage")]
    [InlineData("unknown-location", "location_on")]
    public async Task GetLocationDashboardAsync_ReturnsCorrectLocationIcon(string locationName, string expectedIcon)
    {
        // Arrange
        await SeedNodeWithReadings("temperature", locationName);

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var location = result.Locations.FirstOrDefault(l => l.LocationName.Equals(locationName, StringComparison.OrdinalIgnoreCase));
        location.Should().NotBeNull();
        location!.LocationIcon.Should().Be(expectedIcon);
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WithMultipleLocations_GroupsCorrectly()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Schlafzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Locations.Should().HaveCount(2);
        result.Locations.Should().Contain(l => l.LocationName == "Wohnzimmer");
        result.Locations.Should().Contain(l => l.LocationName == "Schlafzimmer");
    }

    [Fact]
    public async Task GetLocationDashboardAsync_HeroLocationsFirst()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Außen");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Locations.First().LocationName.Should().Be("Außen"); // Hero locations first
    }

    [Theory]
    [InlineData(SparklinePeriod.Hour)]
    [InlineData(SparklinePeriod.Day)]
    [InlineData(SparklinePeriod.Week)]
    public async Task GetLocationDashboardAsync_WithDifferentPeriods_ReturnsData(SparklinePeriod period)
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync(period);

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WidgetContainsSparklineData()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer", readingCount: 10);

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widget = result.Locations.First().Widgets.First();
        widget.DataPoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLocationDashboardAsync_WidgetContainsMinMaxValues()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer", readingCount: 5);

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widget = result.Locations.First().Widgets.First();
        widget.MinMax.Should().NotBeNull();
    }

    [Fact]
    public async Task GetLocationDashboardAsync_FiltersInvalidMeasurementTypes()
    {
        // Arrange - BME280 and DHT22 are sensor model names, not valid measurement types
        await SeedNodeWithReadings("bme280", "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Locations.Should().BeEmpty(); // bme280 is not a valid measurement type
    }

    [Theory]
    [InlineData("temperature")]
    [InlineData("humidity")]
    [InlineData("pressure")]
    [InlineData("co2")]
    public async Task GetLocationDashboardAsync_AcceptsValidMeasurementTypes(string measurementType)
    {
        // Arrange
        await SeedNodeWithReadings(measurementType, "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        result.Locations.Should().NotBeEmpty();
        result.Locations.First().Widgets.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetLocationDashboardAsync_SetsCorrectColorForMeasurementType()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widget = result.Locations.First().Widgets.First();
        widget.Color.Should().Be("#FF5722"); // Orange for temperature
    }

    #endregion

    #region GetFilteredDashboardAsync Tests

    [Fact]
    public async Task GetFilteredDashboardAsync_WithNoFilters_ReturnsAllData()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Schlafzimmer");

        var filter = new DashboardFilterDto();

        // Act
        var result = await _service.GetFilteredDashboardAsync(filter);

        // Assert
        result.Locations.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFilteredDashboardAsync_WithLocationFilter_FiltersCorrectly()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Schlafzimmer");

        var filter = new DashboardFilterDto(Locations: new[] { "Wohnzimmer" });

        // Act
        var result = await _service.GetFilteredDashboardAsync(filter);

        // Assert
        result.Locations.Should().HaveCount(1);
        result.Locations.First().LocationName.Should().Be("Wohnzimmer");
    }

    [Fact]
    public async Task GetFilteredDashboardAsync_WithMeasurementTypeFilter_FiltersCorrectly()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Wohnzimmer");

        var filter = new DashboardFilterDto(MeasurementTypes: new[] { "temperature" });

        // Act
        var result = await _service.GetFilteredDashboardAsync(filter);

        // Assert
        result.Locations.Should().HaveCount(1);
        result.Locations.First().Widgets.Should().HaveCount(1);
        result.Locations.First().Widgets.First().MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public async Task GetFilteredDashboardAsync_WithMultipleFilters_AppliesBoth()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Wohnzimmer");
        await SeedNodeWithReadings("temperature", "Schlafzimmer");

        var filter = new DashboardFilterDto(
            Locations: new[] { "Wohnzimmer" },
            MeasurementTypes: new[] { "temperature" }
        );

        // Act
        var result = await _service.GetFilteredDashboardAsync(filter);

        // Assert
        result.Locations.Should().HaveCount(1);
        result.Locations.First().Widgets.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetFilteredDashboardAsync_WithPeriodFilter_UsesCorrectTimeRange()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");

        var filter = new DashboardFilterDto(Period: SparklinePeriod.Hour);

        // Act
        var result = await _service.GetFilteredDashboardAsync(filter);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFilteredDashboardAsync_WithNonExistentLocation_ReturnsEmpty()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");

        var filter = new DashboardFilterDto(Locations: new[] { "NonExistentLocation" });

        // Act
        var result = await _service.GetFilteredDashboardAsync(filter);

        // Assert
        result.Locations.Should().BeEmpty();
    }

    #endregion

    #region GetFilterOptionsAsync Tests

    [Fact]
    public async Task GetFilterOptionsAsync_WithNoData_ReturnsEmptyOptions()
    {
        // Act
        var result = await _service.GetFilterOptionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Locations.Should().BeEmpty();
        result.MeasurementTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFilterOptionsAsync_ReturnsAvailableLocations()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Schlafzimmer");

        // Act
        var result = await _service.GetFilterOptionsAsync();

        // Assert
        result.Locations.Should().HaveCount(2);
        result.Locations.Should().Contain("Wohnzimmer");
        result.Locations.Should().Contain("Schlafzimmer");
    }

    [Fact]
    public async Task GetFilterOptionsAsync_ReturnsAvailableMeasurementTypes()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Wohnzimmer");

        // Act
        var result = await _service.GetFilterOptionsAsync();

        // Assert
        result.MeasurementTypes.Should().Contain("temperature");
        result.MeasurementTypes.Should().Contain("humidity");
    }

    [Fact]
    public async Task GetFilterOptionsAsync_FiltersInvalidMeasurementTypes()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("invalid_type", "Wohnzimmer");

        // Act
        var result = await _service.GetFilterOptionsAsync();

        // Assert
        result.MeasurementTypes.Should().Contain("temperature");
        result.MeasurementTypes.Should().NotContain("invalid_type");
    }

    [Fact]
    public async Task GetFilterOptionsAsync_ReturnsUniqueLocations()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Wohnzimmer");

        // Act
        var result = await _service.GetFilterOptionsAsync();

        // Assert
        result.Locations.Should().HaveCount(1); // Only one unique location
    }

    [Fact]
    public async Task GetFilterOptionsAsync_LocationsAreSorted()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Außen");

        // Act
        var result = await _service.GetFilterOptionsAsync();

        // Assert
        result.Locations.Should().BeInAscendingOrder();
    }

    #endregion

    #region Widget Building Tests

    [Fact]
    public async Task BuildWidget_ContainsCorrectNodeInfo()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeName = "Test Node 123";
        await SeedNodeWithReadings("temperature", "Wohnzimmer", nodeId: nodeId, nodeName: nodeName);

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widget = result.Locations.First().Widgets.First();
        widget.NodeId.Should().Be(nodeId);
        widget.NodeName.Should().Be(nodeName);
    }

    [Fact]
    public async Task BuildWidget_ContainsCorrectSensorInfo()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widget = result.Locations.First().Widgets.First();
        widget.SensorName.Should().NotBeNullOrEmpty();
        widget.Unit.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task BuildWidget_CurrentValueIsLatestReading()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        await SeedNodeWithReadings("temperature", "Wohnzimmer", nodeId: nodeId, assignmentId: assignmentId);

        // Add a newer reading
        _context.Readings.Add(new Reading
        {
            Id = new Random().NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 99.99,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widget = result.Locations.First().Widgets.First();
        widget.CurrentValue.Should().Be(99.99);
    }

    [Fact]
    public async Task BuildWidget_WidgetIdIsUnique()
    {
        // Arrange
        await SeedNodeWithReadings("temperature", "Wohnzimmer");
        await SeedNodeWithReadings("humidity", "Wohnzimmer");

        // Act
        var result = await _service.GetLocationDashboardAsync();

        // Assert
        var widgets = result.Locations.First().Widgets.ToList();
        var widgetIds = widgets.Select(w => w.WidgetId).ToList();
        widgetIds.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Helper Methods

    private async Task SeedNodeWithReadings(
        string measurementType,
        string locationName,
        int readingCount = 5,
        Guid? nodeId = null,
        string? nodeName = null,
        Guid? assignmentId = null)
    {
        var actualNodeId = nodeId ?? Guid.NewGuid();
        var actualAssignmentId = assignmentId ?? Guid.NewGuid();
        var sensorId = Guid.NewGuid();

        var sensor = new Sensor
        {
            Id = sensorId,
            Name = "BME280",
            Code = $"bme280_{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow,
            Capabilities = new List<SensorCapability>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = sensorId,
                    MeasurementType = measurementType,
                    Unit = measurementType == "temperature" ? "°C" : "%"
                }
            }
        };
        _context.Sensors.Add(sensor);

        var node = new Node
        {
            Id = actualNodeId,
            HubId = _hubId,
            NodeId = $"node-{Guid.NewGuid():N}",
            Name = nodeName ?? "Test Node",
            MacAddress = $"{Guid.NewGuid():N}".Substring(0, 12),
            Location = new Location(locationName),
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            SensorAssignments = new List<NodeSensorAssignment>
            {
                new()
                {
                    Id = actualAssignmentId,
                    NodeId = actualNodeId,
                    SensorId = sensorId,
                    EndpointId = 1,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                }
            }
        };
        _context.Nodes.Add(node);

        // Add readings
        var random = new Random();
        for (var i = 0; i < readingCount; i++)
        {
            var reading = new Reading
            {
                Id = random.NextInt64(),
                TenantId = _tenantId,
                NodeId = actualNodeId,
                AssignmentId = actualAssignmentId,
                MeasurementType = measurementType,
                Value = Math.Round(20 + random.NextDouble() * 10, 2),
                Unit = measurementType == "temperature" ? "°C" : "%",
                Timestamp = DateTime.UtcNow.AddMinutes(-i * 5)
            };
            _context.Readings.Add(reading);
        }

        await _context.SaveChangesAsync();
    }

    #endregion
}
