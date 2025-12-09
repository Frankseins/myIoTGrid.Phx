using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for ReadingService (Measurement management).
/// New model: Reading uses AssignmentId + MeasurementType, stores both RawValue and calibrated Value.
/// Three-tier model: SensorType → Sensor → NodeSensorAssignment → Reading
/// </summary>
public class ReadingServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly ReadingService _sut;
    private readonly Mock<ILogger<ReadingService>> _loggerMock;
    private readonly Mock<INodeService> _nodeServiceMock;
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<IHubService> _hubServiceMock;
    private readonly Mock<IEffectiveConfigService> _effectiveConfigMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly UnitOfWork _unitOfWork;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId;
    private readonly Guid _nodeId;
    private readonly Guid _sensorId;
    private readonly Guid _assignmentId;

    public ReadingServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<ReadingService>>();
        _nodeServiceMock = new Mock<INodeService>();
        _tenantServiceMock = new Mock<ITenantService>();
        _hubServiceMock = new Mock<IHubService>();
        _effectiveConfigMock = new Mock<IEffectiveConfigService>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        _unitOfWork = new UnitOfWork(_context);

        // Setup TenantService
        _tenantServiceMock.Setup(x => x.GetCurrentTenantId()).Returns(_tenantId);

        // Create Hub
        _hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = _hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });

        // Create Node
        _nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = _nodeId,
            HubId = _hubId,
            NodeId = "test-node",
            Name = "Test Node",
            Location = new Location("Wohnzimmer", null, null),
            CreatedAt = DateTime.UtcNow
        });

        // Create Sensor with Capabilities (v3.0 Two-Tier: Sensor has Code/Name/Capabilities directly)
        _sensorId = Guid.NewGuid();
        var sensor = new Sensor
        {
            Id = _sensorId,
            TenantId = _tenantId,
            Code = "dht22-living-room",
            Name = "Living Room DHT22",
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

        sensor.Capabilities.Add(new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = _sensorId,
            MeasurementType = "temperature",
            DisplayName = "Temperature",
            Unit = "°C",
            MinValue = -40,
            MaxValue = 80,
            MatterClusterId = 0x0402,
            MatterClusterName = "TemperatureMeasurement",
            IsActive = true
        });

        sensor.Capabilities.Add(new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = _sensorId,
            MeasurementType = "humidity",
            DisplayName = "Humidity",
            Unit = "%",
            MinValue = 0,
            MaxValue = 100,
            MatterClusterId = 0x0405,
            MatterClusterName = "RelativeHumidityMeasurement",
            IsActive = true
        });

        _context.Sensors.Add(sensor);

        // Create NodeSensorAssignment
        _assignmentId = Guid.NewGuid();
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = _assignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        _context.SaveChanges();

        // Default calibration: return raw value unchanged (v3.0 Two-Tier: no SensorType parameter)
        _effectiveConfigMock.Setup(x => x.ApplyCalibration(It.IsAny<double>(), It.IsAny<Sensor>()))
            .Returns((double raw, Sensor s) => raw);

        _sut = new ReadingService(
            _context,
            _unitOfWork,
            _nodeServiceMock.Object,
            _tenantServiceMock.Object,
            _hubServiceMock.Object,
            _effectiveConfigMock.Object,
            _signalRMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingReading_ReturnsReading()
    {
        // Arrange
        var reading = CreateTestReading(21.5);
        _context.Readings.Add(reading);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(reading.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(21.5);
        result.MeasurementType.Should().Be("temperature");
        result.Unit.Should().Be("°C");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingReading_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_IncludesLocation()
    {
        // Arrange
        var reading = CreateTestReading(21.5);
        _context.Readings.Add(reading);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(reading.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Location.Should().NotBeNull();
        result.Location!.Name.Should().Be("Wohnzimmer");
    }

    #endregion

    #region GetByNodeAsync Tests

    [Fact]
    public async Task GetByNodeAsync_ReturnsReadingsForNode()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5, "temperature"));
        _context.Readings.Add(CreateTestReading(65.0, "humidity"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByNodeAsync_WithMeasurementTypeFilter_FiltersResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5, "temperature"));
        _context.Readings.Add(CreateTestReading(65.0, "humidity"));
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(MeasurementType: "temperature");

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId, filter);

        // Assert
        result.Should().ContainSingle();
        result.First().MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public async Task GetByNodeAsync_WithDateFilter_FiltersResults()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(21.5, timestamp: now.AddHours(-2)));
        _context.Readings.Add(CreateTestReading(22.5, timestamp: now));
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(From: now.AddHours(-1));

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId, filter);

        // Assert
        result.Should().ContainSingle();
        result.First().Value.Should().Be(22.5);
    }

    [Fact]
    public async Task GetByNodeAsync_WithAssignmentFilter_FiltersResults()
    {
        // Arrange
        var otherAssignmentId = Guid.NewGuid();
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = otherAssignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 2,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        _context.Readings.Add(CreateTestReading(21.5));
        var otherReading = CreateTestReading(65.0);
        otherReading.AssignmentId = otherAssignmentId;
        _context.Readings.Add(otherReading);
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(AssignmentId: _assignmentId);

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId, filter);

        // Assert
        result.Should().ContainSingle();
        result.First().Value.Should().Be(21.5);
    }

    [Fact]
    public async Task GetByNodeAsync_OrdersByTimestampDescending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(20.0, timestamp: now.AddHours(-2)));
        _context.Readings.Add(CreateTestReading(22.0, timestamp: now));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByNodeAsync(_nodeId)).ToList();

        // Assert
        result[0].Value.Should().Be(22.0); // Most recent first
        result[1].Value.Should().Be(20.0);
    }

    #endregion

    #region GetFilteredAsync Tests

    [Fact]
    public async Task GetFilteredAsync_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.Readings.Add(CreateTestReading(20.0 + i, timestamp: DateTime.UtcNow.AddMinutes(-i)));
        }
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(Page: 1, PageSize: 5);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(10);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetFilteredAsync_WithNodeIdFilter_FiltersResults()
    {
        // Arrange
        var otherNodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = otherNodeId,
            HubId = _hubId,
            NodeId = "other-node",
            Name = "Other Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Readings.Add(CreateTestReading(21.5));
        var otherReading = CreateTestReading(22.5);
        otherReading.NodeId = otherNodeId;
        _context.Readings.Add(otherReading);
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(NodeId: _nodeId);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Value.Should().Be(21.5);
    }

    [Fact]
    public async Task GetFilteredAsync_WithIsSyncedFilter_FiltersResults()
    {
        // Arrange
        var syncedReading = CreateTestReading(21.5);
        syncedReading.IsSyncedToCloud = true;
        var unsyncedReading = CreateTestReading(22.5);
        unsyncedReading.IsSyncedToCloud = false;

        _context.Readings.Add(syncedReading);
        _context.Readings.Add(unsyncedReading);
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(IsSyncedToCloud: false);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public async Task GetFilteredAsync_WithHubIdFilter_FiltersResults()
    {
        // Arrange
        var otherHubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = otherHubId,
            TenantId = _tenantId,
            HubId = "other-hub",
            Name = "Other Hub",
            CreatedAt = DateTime.UtcNow
        });
        var otherNodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = otherNodeId,
            HubId = otherHubId,
            NodeId = "other-node",
            Name = "Other Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Readings.Add(CreateTestReading(21.5));
        var otherReading = CreateTestReading(22.5);
        otherReading.NodeId = otherNodeId;
        _context.Readings.Add(otherReading);
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(HubId: _hubId);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Value.Should().Be(21.5);
    }

    [Fact]
    public async Task GetFilteredAsync_WithMeasurementTypeFilter_FiltersResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5, "temperature"));
        _context.Readings.Add(CreateTestReading(65.0, "humidity"));
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(MeasurementType: "temperature");

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().MeasurementType.Should().Be("temperature");
    }

    #endregion

    #region GetLatestByNodeAsync Tests

    [Fact]
    public async Task GetLatestByNodeAsync_ReturnsLatestPerMeasurementType()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(20.0, "temperature", now.AddHours(-1)));
        _context.Readings.Add(CreateTestReading(22.0, "temperature", now)); // Latest temp
        _context.Readings.Add(CreateTestReading(65.0, "humidity", now));
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetLatestByNodeAsync(_nodeId)).ToList();

        // Assert
        result.Should().HaveCount(2); // One per MeasurementType
        result.First(r => r.MeasurementType == "temperature").Value.Should().Be(22.0);
        result.First(r => r.MeasurementType == "humidity").Value.Should().Be(65.0);
    }

    [Fact]
    public async Task GetLatestByNodeAsync_WhenNoReadings_ReturnsEmpty()
    {
        // Act
        var result = await _sut.GetLatestByNodeAsync(_nodeId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetLatestAsync Tests

    [Fact]
    public async Task GetLatestAsync_ReturnsLatestPerNodeAndMeasurementType()
    {
        // Arrange
        var otherNodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = otherNodeId,
            HubId = _hubId,
            NodeId = "other-node",
            Name = "Other Node",
            CreatedAt = DateTime.UtcNow
        });

        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(21.5, "temperature", now));
        var otherReading = CreateTestReading(22.5, "temperature", now);
        otherReading.NodeId = otherNodeId;
        _context.Readings.Add(otherReading);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetLatestAsync()).ToList();

        // Assert
        result.Should().HaveCount(2); // One per Node+MeasurementType
    }

    #endregion

    #region GetUnsyncedAsync Tests

    [Fact]
    public async Task GetUnsyncedAsync_ReturnsOnlyUnsyncedReadings()
    {
        // Arrange
        var unsyncedReading = CreateTestReading(21.5);
        unsyncedReading.IsSyncedToCloud = false;
        var syncedReading = CreateTestReading(22.5);
        syncedReading.IsSyncedToCloud = true;

        _context.Readings.Add(unsyncedReading);
        _context.Readings.Add(syncedReading);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnsyncedAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnsyncedAsync_RespectsLimit()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var reading = CreateTestReading(20.0 + i, timestamp: DateTime.UtcNow.AddMinutes(-i));
            reading.IsSyncedToCloud = false;
            _context.Readings.Add(reading);
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetUnsyncedAsync(limit: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetUnsyncedAsync_OrdersByTimestampAscending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var reading1 = CreateTestReading(22.0, timestamp: now);
        reading1.IsSyncedToCloud = false;
        var reading2 = CreateTestReading(20.0, timestamp: now.AddHours(-1));
        reading2.IsSyncedToCloud = false;

        _context.Readings.Add(reading1);
        _context.Readings.Add(reading2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetUnsyncedAsync()).ToList();

        // Assert
        result[0].Value.Should().Be(20.0); // Oldest first
        result[1].Value.Should().Be(22.0);
    }

    #endregion

    #region MarkAsSyncedAsync Tests

    [Fact(Skip = "ExecuteUpdateAsync is not supported by InMemory provider")]
    public async Task MarkAsSyncedAsync_MarksReadingsAsSynced()
    {
        // Arrange
        var reading1 = CreateTestReading(21.5);
        reading1.IsSyncedToCloud = false;
        var reading2 = CreateTestReading(65.0, "humidity");
        reading2.IsSyncedToCloud = false;

        _context.Readings.Add(reading1);
        _context.Readings.Add(reading2);
        await _context.SaveChangesAsync();

        // Act
        await _sut.MarkAsSyncedAsync(new[] { reading1.Id, reading2.Id });

        // Refresh from database
        _context.ChangeTracker.Clear();
        var result1 = await _context.Readings.FindAsync(reading1.Id);
        var result2 = await _context.Readings.FindAsync(reading2.Id);

        // Assert
        result1!.IsSyncedToCloud.Should().BeTrue();
        result2!.IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsSyncedAsync_WithEmptyList_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.MarkAsSyncedAsync(Array.Empty<long>());
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesReadingWithCalibration()
    {
        // Arrange
        var hubDto = CreateHubDto();
        var nodeDto = CreateNodeDto();

        _hubServiceMock.Setup(x => x.GetOrCreateByHubIdAsync("test-hub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hubDto);
        _nodeServiceMock.Setup(x => x.GetOrCreateByNodeIdAsync(_hubId, "test-node", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeDto);
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Calibration: RawValue * 1.02 + 0.5 = 22.33 (v3.0 Two-Tier: no SensorType parameter)
        _effectiveConfigMock.Setup(x => x.ApplyCalibration(21.5, It.IsAny<Sensor>()))
            .Returns(22.33);

        var dto = new CreateReadingDto(
            NodeId: "test-node",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: "test-hub"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.RawValue.Should().Be(21.5);
        result.Value.Should().Be(22.33); // Calibrated value
        result.MeasurementType.Should().Be("temperature");
        result.Unit.Should().Be("°C");
        result.IsSyncedToCloud.Should().BeFalse();
        result.AssignmentId.Should().Be(_assignmentId);
    }

    [Fact]
    public async Task CreateAsync_WithUnknownEndpoint_CreatesReadingWithoutAssignment()
    {
        // Arrange
        var hubDto = CreateHubDto();
        var nodeDto = CreateNodeDto();

        _hubServiceMock.Setup(x => x.GetOrCreateByHubIdAsync("test-hub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hubDto);
        _nodeServiceMock.Setup(x => x.GetOrCreateByNodeIdAsync(_hubId, "test-node", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeDto);
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateReadingDto(
            NodeId: "test-node",
            EndpointId: 99, // Unknown endpoint
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: "test-hub"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.RawValue.Should().Be(21.5);
        result.Value.Should().Be(21.5); // No calibration applied
        result.AssignmentId.Should().BeNull();
        result.Unit.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithTimestamp_UsesProvidedTimestamp()
    {
        // Arrange
        var timestamp = DateTime.UtcNow.AddHours(-1);
        var hubDto = CreateHubDto();
        var nodeDto = CreateNodeDto();

        _hubServiceMock.Setup(x => x.GetOrCreateByHubIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hubDto);
        _nodeServiceMock.Setup(x => x.GetOrCreateByNodeIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeDto);
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateReadingDto(
            NodeId: "test-node",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: "test-hub",
            Timestamp: timestamp
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public async Task CreateAsync_NotifiesSignalR()
    {
        // Arrange
        var hubDto = CreateHubDto();
        var nodeDto = CreateNodeDto();

        _hubServiceMock.Setup(x => x.GetOrCreateByHubIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hubDto);
        _nodeServiceMock.Setup(x => x.GetOrCreateByNodeIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeDto);
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateReadingDto(
            NodeId: "test-node",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: "test-hub"
        );

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _signalRMock.Verify(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_UpdatesNodeLastSeen()
    {
        // Arrange
        var hubDto = CreateHubDto();
        var nodeDto = CreateNodeDto();

        _hubServiceMock.Setup(x => x.GetOrCreateByHubIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hubDto);
        _nodeServiceMock.Setup(x => x.GetOrCreateByNodeIdAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeDto);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateReadingDto(
            NodeId: "test-node",
            EndpointId: 1,
            MeasurementType: "temperature",
            RawValue: 21.5,
            HubId: "test-hub"
        );

        // Act
        await _sut.CreateAsync(dto);

        // Assert
        _nodeServiceMock.Verify(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateFromSensorAsync Tests

    [Fact]
    public async Task CreateFromSensorAsync_WithGuidDeviceId_FindsNodeByGuid()
    {
        // Arrange
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateSensorReadingDto(
            DeviceId: _nodeId.ToString(),
            Type: "temperature",
            Value: 21.5,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Unit: "°C"
        );

        // Act
        var result = await _sut.CreateFromSensorAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(21.5);
        result.MeasurementType.Should().Be("temperature");
        result.NodeId.Should().Be(_nodeId);
    }

    [Fact]
    public async Task CreateFromSensorAsync_WithNodeIdString_FindsNodeByNodeId()
    {
        // Arrange
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateSensorReadingDto(
            DeviceId: "test-node",
            Type: "humidity",
            Value: 65.0,
            Unit: "%"
        );

        // Act
        var result = await _sut.CreateFromSensorAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(65.0);
        result.MeasurementType.Should().Be("humidity");
    }

    [Fact]
    public async Task CreateFromSensorAsync_WithUnknownDeviceId_ThrowsException()
    {
        // Arrange
        var dto = new CreateSensorReadingDto(
            DeviceId: "unknown-device",
            Type: "temperature",
            Value: 21.5
        );

        // Act & Assert
        var act = () => _sut.CreateFromSensorAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task CreateFromSensorAsync_WithTimestamp_ConvertsFromUnixTime()
    {
        // Arrange
        var expectedTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var unixTimestamp = new DateTimeOffset(expectedTime).ToUnixTimeSeconds();

        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateSensorReadingDto(
            DeviceId: _nodeId.ToString(),
            Type: "temperature",
            Value: 21.5,
            Timestamp: unixTimestamp
        );

        // Act
        var result = await _sut.CreateFromSensorAsync(dto);

        // Assert
        result.Timestamp.Should().Be(expectedTime);
    }

    [Fact]
    public async Task CreateFromSensorAsync_WithoutTimestamp_UsesCurrentTime()
    {
        // Arrange
        var beforeTest = DateTime.UtcNow;

        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateSensorReadingDto(
            DeviceId: _nodeId.ToString(),
            Type: "temperature",
            Value: 21.5
        );

        // Act
        var result = await _sut.CreateFromSensorAsync(dto);

        // Assert
        result.Timestamp.Should().BeOnOrAfter(beforeTest);
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateFromSensorAsync_NotifiesSignalR()
    {
        // Arrange
        _nodeServiceMock.Setup(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateSensorReadingDto(
            DeviceId: _nodeId.ToString(),
            Type: "temperature",
            Value: 21.5
        );

        // Act
        await _sut.CreateFromSensorAsync(dto);

        // Assert
        _signalRMock.Verify(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromSensorAsync_UpdatesNodeLastSeen()
    {
        // Arrange
        _signalRMock.Setup(x => x.NotifyNewReadingAsync(It.IsAny<ReadingDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new CreateSensorReadingDto(
            DeviceId: _nodeId.ToString(),
            Type: "temperature",
            Value: 21.5
        );

        // Act
        await _sut.CreateFromSensorAsync(dto);

        // Assert
        _nodeServiceMock.Verify(x => x.UpdateLastSeenAsync(_nodeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
            _context.Readings.Add(CreateTestReading(20.0 + i));
        }
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Page = 0, Size = 10 };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalRecords.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithSearch_FiltersResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5, "temperature"));
        _context.Readings.Add(CreateTestReading(65.0, "humidity"));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "temperature" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().MeasurementType.Should().Be("temperature");
    }

    [Fact]
    public async Task GetPagedAsync_WithNodeIdFilter_FiltersResults()
    {
        // Arrange
        var otherNodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = otherNodeId,
            HubId = _hubId,
            NodeId = "other-node",
            Name = "Other Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Readings.Add(CreateTestReading(21.5));
        var otherReading = CreateTestReading(22.5);
        otherReading.NodeId = otherNodeId;
        _context.Readings.Add(otherReading);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "nodeId", _nodeId.ToString() } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().NodeId.Should().Be(_nodeId);
    }

    [Fact]
    public async Task GetPagedAsync_WithMeasurementTypeFilter_FiltersResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5, "temperature"));
        _context.Readings.Add(CreateTestReading(65.0, "humidity"));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "measurementType", "humidity" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().MeasurementType.Should().Be("humidity");
    }

    [Fact]
    public async Task GetPagedAsync_WithSyncedFilter_FiltersResults()
    {
        // Arrange
        var syncedReading = CreateTestReading(21.5);
        syncedReading.IsSyncedToCloud = true;
        var unsyncedReading = CreateTestReading(22.5);
        unsyncedReading.IsSyncedToCloud = false;

        _context.Readings.Add(syncedReading);
        _context.Readings.Add(unsyncedReading);
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "isSyncedToCloud", "true" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().IsSyncedToCloud.Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_WithDateFilter_FiltersResults()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(21.5, timestamp: now.AddDays(-5)));
        _context.Readings.Add(CreateTestReading(22.0, timestamp: now.AddDays(-2)));
        _context.Readings.Add(CreateTestReading(22.5, timestamp: now));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            DateFrom = now.AddDays(-3),
            DateTo = now.AddDays(1)
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_WithSort_SortsResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(25.0, timestamp: DateTime.UtcNow.AddHours(-1)));
        _context.Readings.Add(CreateTestReading(20.0, timestamp: DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Sort = "Value" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.First().Value.Should().Be(20.0);
        result.Items.Last().Value.Should().Be(25.0);
    }

    [Fact]
    public async Task GetPagedAsync_DefaultSorting_OrdersByTimestampAsc()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(20.0, timestamp: now.AddHours(-1)));
        _context.Readings.Add(CreateTestReading(25.0, timestamp: now));
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto();

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert - Default sorting is ascending (oldest first)
        result.Items.First().Value.Should().Be(20.0); // Oldest first
        result.Items.Last().Value.Should().Be(25.0);
    }

    #endregion

    #region GetFilteredAsync Additional Tests

    [Fact]
    public async Task GetFilteredAsync_WithNodeIdentifierFilter_FiltersResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5));
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(NodeIdentifier: "test-node");

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task GetFilteredAsync_WithAssignmentIdFilter_FiltersResults()
    {
        // Arrange
        _context.Readings.Add(CreateTestReading(21.5));
        var otherReading = CreateTestReading(22.0);
        otherReading.AssignmentId = Guid.NewGuid();
        _context.Readings.Add(otherReading);
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(AssignmentId: _assignmentId);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Value.Should().Be(21.5);
    }

    [Fact]
    public async Task GetFilteredAsync_WithIsSyncedToCloudFilter_FiltersResults()
    {
        // Arrange
        var syncedReading = CreateTestReading(21.5);
        syncedReading.IsSyncedToCloud = true;
        var unsyncedReading = CreateTestReading(22.0);
        unsyncedReading.IsSyncedToCloud = false;

        _context.Readings.Add(syncedReading);
        _context.Readings.Add(unsyncedReading);
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(IsSyncedToCloud: false);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().IsSyncedToCloud.Should().BeFalse();
    }

    [Fact]
    public async Task GetFilteredAsync_WithDateRangeFilter_FiltersResults()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.Readings.Add(CreateTestReading(20.0, timestamp: now.AddDays(-5)));
        _context.Readings.Add(CreateTestReading(21.5, timestamp: now.AddDays(-2)));
        _context.Readings.Add(CreateTestReading(22.0, timestamp: now));
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(From: now.AddDays(-3), To: now.AddDays(-1));

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Value.Should().Be(21.5);
    }

    [Fact]
    public async Task GetFilteredAsync_Pagination_WorksCorrectly()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            _context.Readings.Add(CreateTestReading(20.0 + i));
        }
        await _context.SaveChangesAsync();

        var filter = new ReadingFilterDto(Page: 2, PageSize: 10);

        // Act
        var result = await _sut.GetFilteredAsync(filter);

        // Assert
        result.TotalCount.Should().Be(25);
        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
    }

    #endregion

    #region Helper Methods

    private Reading CreateTestReading(double value, string measurementType = "temperature", DateTime? timestamp = null)
    {
        return new Reading
        {
            TenantId = _tenantId,
            NodeId = _nodeId,
            AssignmentId = _assignmentId,
            MeasurementType = measurementType,
            RawValue = value,
            Value = value,
            Unit = measurementType == "temperature" ? "°C" : "%",
            Timestamp = timestamp ?? DateTime.UtcNow,
            IsSyncedToCloud = false
        };
    }

    private HubDto CreateHubDto()
    {
        return new HubDto(
            Id: _hubId,
            TenantId: _tenantId,
            HubId: "test-hub",
            Name: "Test Hub",
            Description: null,
            LastSeen: null,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 0
        );
    }

    private NodeDto CreateNodeDto()
    {
        return new NodeDto(
            Id: _nodeId,
            HubId: _hubId,
            NodeId: "test-node",
            Name: "Test Node",
            Protocol: ProtocolDto.WLAN,
            Location: new LocationDto("Wohnzimmer", null, null),
            AssignmentCount: 1,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: null,
            BatteryLevel: null,
            CreatedAt: DateTime.UtcNow,
            MacAddress: "AA:BB:CC:DD:EE:FF",
            Status: NodeProvisioningStatusDto.Configured,
            IsSimulation: false,
            StorageMode: StorageModeDto.RemoteOnly,
            PendingSyncCount: 0,
            LastSyncAt: null,
            LastSyncError: null,
            DebugLevel: DebugLevelDto.Normal,
            EnableRemoteLogging: false,
            LastDebugChange: null
        );
    }

    #endregion
}
