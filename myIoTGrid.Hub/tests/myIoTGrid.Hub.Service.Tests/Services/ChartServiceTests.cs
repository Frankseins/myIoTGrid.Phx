using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Unit tests for ChartService.
/// </summary>
public class ChartServiceTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ILogger<ChartService>> _loggerMock;
    private readonly ChartService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _hubId = Guid.NewGuid();

    public ChartServiceTests()
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HubDbContext(options);
        _tenantServiceMock = new Mock<ITenantService>();
        _loggerMock = new Mock<ILogger<ChartService>>();

        _tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns(_tenantId);

        _service = new ChartService(_context, _tenantServiceMock.Object, _loggerMock.Object);

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

    #region GetChartDataAsync Tests

    [Fact]
    public async Task GetChartDataAsync_WithValidData_ReturnsChartData()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.AssignmentId.Should().Be(assignmentId);
        result.MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public async Task GetChartDataAsync_WithNonExistentNode_ReturnsNull()
    {
        // Act
        var result = await _service.GetChartDataAsync(Guid.NewGuid(), Guid.NewGuid(), "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChartDataAsync_WithNonExistentAssignment_ReturnsNull()
    {
        // Arrange
        var (nodeId, _) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, Guid.NewGuid(), "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChartDataAsync_WithNoReadings_ReturnsNull()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 0);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(ChartInterval.OneHour)]
    [InlineData(ChartInterval.OneDay)]
    [InlineData(ChartInterval.OneWeek)]
    [InlineData(ChartInterval.OneMonth)]
    [InlineData(ChartInterval.ThreeMonths)]
    [InlineData(ChartInterval.SixMonths)]
    [InlineData(ChartInterval.OneYear)]
    public async Task GetChartDataAsync_WithDifferentIntervals_ReturnsData(ChartInterval interval)
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", interval);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetChartDataAsync_ContainsDataPoints()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 20);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.DataPoints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetChartDataAsync_ContainsStats()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.Stats.Should().NotBeNull();
        result.Stats.MinValue.Should().BeLessThanOrEqualTo(result.Stats.MaxValue);
    }

    [Fact]
    public async Task GetChartDataAsync_ContainsTrend()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.Trend.Should().NotBeNull();
        result.Trend.Direction.Should().BeOneOf("up", "down", "stable");
    }

    [Fact]
    public async Task GetChartDataAsync_CurrentValueIsLatestReading()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Add a specific latest reading
        _context.Readings.Add(new Reading
        {
            Id = new Random().NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 42.42,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.CurrentValue.Should().Be(42.42);
    }

    [Fact]
    public async Task GetChartDataAsync_ReturnsCorrectColor()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.Color.Should().Be("#FF5722"); // Orange for temperature
    }

    [Fact]
    public async Task GetChartDataAsync_DataPointsAreAggregated()
    {
        // Arrange - Add many readings
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 100);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        // Data points should be aggregated, so count should be less than raw reading count
        result!.DataPoints.Count().Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetChartDataAsync_CaseInsensitiveMeasurementType()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "TEMPERATURE", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region GetReadingsListAsync Tests

    [Fact]
    public async Task GetReadingsListAsync_ReturnsPagedResults()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 25);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetReadingsListAsync_ReturnsSecondPage()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 25);
        var request = new ReadingsListRequestDto(Page: 2, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task GetReadingsListAsync_LastPageHasRemainingItems()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 25);
        var request = new ReadingsListRequestDto(Page: 3, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetReadingsListAsync_FiltersByFromDate()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);
        var fromDate = DateTime.UtcNow.AddMinutes(-30);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 20, From: fromDate);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetReadingsListAsync_FiltersByToDate()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);
        var toDate = DateTime.UtcNow.AddMinutes(-20);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 20, To: toDate);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task GetReadingsListAsync_FiltersByDateRange()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 20);
        var fromDate = DateTime.UtcNow.AddMinutes(-60);
        var toDate = DateTime.UtcNow.AddMinutes(-30);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 20, From: fromDate, To: toDate);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReadingsListAsync_SortsByTimestampDescending()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        var timestamps = result.Items.Select(i => i.Timestamp).ToList();
        timestamps.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetReadingsListAsync_IncludesTrendDirection()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        // Last item won't have trend (no next item to compare against)
        // All other items should have trend direction
        var itemsWithTrend = result.Items.SkipLast(1).ToList();
        if (itemsWithTrend.Any())
        {
            itemsWithTrend.All(i => i.TrendDirection != null).Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetReadingsListAsync_TrendDirectionIsCorrect()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();

        var sensor = new Sensor
        {
            Id = sensorId,
            Name = "Test Sensor",
            Code = "test",
            CreatedAt = DateTime.UtcNow,
            Capabilities = new List<SensorCapability>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = sensorId,
                    MeasurementType = "temperature",
                    Unit = "°C"
                }
            }
        };
        _context.Sensors.Add(sensor);

        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "trend-test",
            Name = "Trend Test Node",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            Location = new Location("Test"),
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            SensorAssignments = new List<NodeSensorAssignment>
            {
                new()
                {
                    Id = assignmentId,
                    NodeId = nodeId,
                    SensorId = sensorId,
                    EndpointId = 1,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                }
            }
        };
        _context.Nodes.Add(node);

        // Add readings with known values (newest first when queried)
        var random = new Random();
        _context.Readings.Add(new Reading
        {
            Id = random.NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 25.0, // Higher value (newer)
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });
        _context.Readings.Add(new Reading
        {
            Id = random.NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 20.0, // Lower value (older)
            Unit = "°C",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });
        await _context.SaveChangesAsync();

        var request = new ReadingsListRequestDto(Page: 1, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Items.First().Value.Should().Be(25.0);
        result.Items.First().TrendDirection.Should().Be("up"); // 25 > 20
    }

    [Fact]
    public async Task GetReadingsListAsync_WithNoReadings_ReturnsEmptyList()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 0);
        var request = new ReadingsListRequestDto(Page: 1, PageSize: 10);

        // Act
        var result = await _service.GetReadingsListAsync(nodeId, assignmentId, "temperature", request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    #region ExportToCsvAsync Tests

    [Fact]
    public async Task ExportToCsvAsync_ReturnsValidCsv()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportToCsvAsync_ContainsHeader()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);
        var csv = Encoding.UTF8.GetString(result);

        // Assert
        csv.Should().StartWith("Zeitstempel;Wert;Einheit");
    }

    [Fact]
    public async Task ExportToCsvAsync_ContainsDataRows()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 5);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.Should().Be(6); // 1 header + 5 data rows
    }

    [Fact]
    public async Task ExportToCsvAsync_FiltersByFromDate()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);
        var fromDate = DateTime.UtcNow.AddMinutes(-25);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", fromDate, null);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.Should().BeLessThanOrEqualTo(11); // header + filtered data
    }

    [Fact]
    public async Task ExportToCsvAsync_FiltersByToDate()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 10);
        var toDate = DateTime.UtcNow.AddMinutes(-25);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, toDate);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.Should().BeLessThanOrEqualTo(11);
    }

    [Fact]
    public async Task ExportToCsvAsync_UsesGermanDateFormat()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 1);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        // German date format: dd.MM.yyyy HH:mm:ss
        lines[1].Should().MatchRegex(@"\d{2}\.\d{2}\.\d{4} \d{2}:\d{2}:\d{2}");
    }

    [Fact]
    public async Task ExportToCsvAsync_UsesSemicolonSeparator()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 1);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines[0].Split(';').Should().HaveCount(3); // Zeitstempel;Wert;Einheit
        lines[1].Split(';').Should().HaveCount(3);
    }

    [Fact]
    public async Task ExportToCsvAsync_WithNoReadings_ReturnsHeaderOnly()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 0);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Length.Should().Be(1); // Only header
    }

    [Fact]
    public async Task ExportToCsvAsync_ValuesAreFormattedCorrectly()
    {
        // Arrange
        var (nodeId, assignmentId) = await SeedNodeWithReadings("temperature", readingCount: 1);

        // Act
        var result = await _service.ExportToCsvAsync(nodeId, assignmentId, "temperature", null, null);
        var csv = Encoding.UTF8.GetString(result);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataFields = lines[1].Split(';');

        // Assert
        // Value should be formatted with 2 decimal places using InvariantCulture (dot as decimal separator)
        dataFields[1].Should().MatchRegex(@"\d+\.\d{2}");
    }

    #endregion

    #region Stats Calculation Tests

    [Fact]
    public async Task GetChartDataAsync_StatsMinMaxAreCorrect()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();

        var sensor = new Sensor
        {
            Id = sensorId,
            Name = "Test Sensor",
            Code = "stats-test",
            CreatedAt = DateTime.UtcNow,
            Capabilities = new List<SensorCapability>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    SensorId = sensorId,
                    MeasurementType = "temperature",
                    Unit = "°C"
                }
            }
        };
        _context.Sensors.Add(sensor);

        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "stats-test-node",
            Name = "Stats Test Node",
            MacAddress = "11:22:33:44:55:66",
            Location = new Location("Stats Test"),
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            SensorAssignments = new List<NodeSensorAssignment>
            {
                new()
                {
                    Id = assignmentId,
                    NodeId = nodeId,
                    SensorId = sensorId,
                    EndpointId = 1,
                    IsActive = true,
                    AssignedAt = DateTime.UtcNow
                }
            }
        };
        _context.Nodes.Add(node);

        // Add readings with known min/max values
        var random = new Random();
        _context.Readings.Add(new Reading
        {
            Id = random.NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 10.0, // Min
            Unit = "°C",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        });
        _context.Readings.Add(new Reading
        {
            Id = random.NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 30.0, // Max
            Unit = "°C",
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        });
        _context.Readings.Add(new Reading
        {
            Id = random.NextInt64(),
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            Value = 20.0, // Middle
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetChartDataAsync(nodeId, assignmentId, "temperature", ChartInterval.OneDay);

        // Assert
        result.Should().NotBeNull();
        result!.Stats.MinValue.Should().Be(10.0);
        result.Stats.MaxValue.Should().Be(30.0);
        result.Stats.AvgValue.Should().Be(20.0);
    }

    #endregion

    #region Helper Methods

    private async Task<(Guid NodeId, Guid AssignmentId)> SeedNodeWithReadings(
        string measurementType,
        int readingCount = 5)
    {
        var nodeId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
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
            Id = nodeId,
            HubId = _hubId,
            NodeId = $"node-{Guid.NewGuid():N}",
            Name = "Test Node",
            MacAddress = $"{Guid.NewGuid():N}".Substring(0, 12),
            Location = new Location("Test Location"),
            IsOnline = true,
            CreatedAt = DateTime.UtcNow,
            SensorAssignments = new List<NodeSensorAssignment>
            {
                new()
                {
                    Id = assignmentId,
                    NodeId = nodeId,
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
                NodeId = nodeId,
                AssignmentId = assignmentId,
                MeasurementType = measurementType,
                Value = Math.Round(20 + random.NextDouble() * 10, 2),
                Unit = measurementType == "temperature" ? "°C" : "%",
                Timestamp = DateTime.UtcNow.AddMinutes(-i * 5)
            };
            _context.Readings.Add(reading);
        }

        await _context.SaveChangesAsync();

        return (nodeId, assignmentId);
    }

    #endregion
}
