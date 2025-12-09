using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for NodeSensorAssignmentService (v3.0 Two-Tier Model).
/// Hardware binding of Sensors to Nodes with pin configuration.
/// </summary>
public class NodeSensorAssignmentServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly NodeSensorAssignmentService _sut;
    private readonly Mock<IEffectiveConfigService> _effectiveConfigMock;
    private readonly Mock<ILogger<NodeSensorAssignmentService>> _loggerMock;
    private readonly UnitOfWork _unitOfWork;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId;
    private readonly Guid _nodeId;
    private readonly Guid _sensorId;

    public NodeSensorAssignmentServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _effectiveConfigMock = new Mock<IEffectiveConfigService>();
        _loggerMock = new Mock<ILogger<NodeSensorAssignmentService>>();
        _unitOfWork = new UnitOfWork(_context);

        // Setup default effective config behavior
        _effectiveConfigMock.Setup(x => x.GetEffectiveConfig(It.IsAny<NodeSensorAssignment>(), It.IsAny<Sensor>()))
            .Returns(new EffectiveConfigDto(60, null, null, null, null, null, 4, null, null, null, 0, 1.0));

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
            CreatedAt = DateTime.UtcNow
        });

        // Create Sensor
        _sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = _sensorId,
            TenantId = _tenantId,
            Code = "dht22-test",
            Name = "Test DHT22",
            Protocol = CommunicationProtocol.OneWire,
            Category = "climate",
            IntervalSeconds = 60,
            MinIntervalSeconds = 2,
            OffsetCorrection = 0,
            GainCorrection = 1.0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.SaveChanges();

        _sut = new NodeSensorAssignmentService(_context, _unitOfWork, _effectiveConfigMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByNodeAsync Tests

    [Fact]
    public async Task GetByNodeAsync_WithNoAssignments_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetByNodeAsync(_nodeId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByNodeAsync_ReturnsAssignmentsForNode()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId);

        // Assert
        result.Should().ContainSingle();
        result.First().NodeId.Should().Be(_nodeId);
    }

    [Fact]
    public async Task GetByNodeAsync_DoesNotReturnOtherNodeAssignments()
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

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = otherNodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNodeAsync(_nodeId);

        // Assert
        result.Should().ContainSingle();
        result.First().NodeId.Should().Be(_nodeId);
    }

    [Fact]
    public async Task GetByNodeAsync_OrdersByEndpointId()
    {
        // Arrange
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 3,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByNodeAsync(_nodeId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].EndpointId.Should().Be(1);
        result[1].EndpointId.Should().Be(3);
    }

    #endregion

    #region GetBySensorAsync Tests

    [Fact]
    public async Task GetBySensorAsync_WithNoAssignments_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetBySensorAsync(_sensorId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBySensorAsync_ReturnsAssignmentsForSensor()
    {
        // Arrange
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetBySensorAsync(_sensorId);

        // Assert
        result.Should().ContainSingle();
        result.First().SensorId.Should().Be(_sensorId);
    }

    [Fact]
    public async Task GetBySensorAsync_OrdersByNodeName()
    {
        // Arrange
        var otherNodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = otherNodeId,
            HubId = _hubId,
            NodeId = "alpha-node",
            Name = "Alpha Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId, // "Test Node"
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = otherNodeId, // "Alpha Node"
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetBySensorAsync(_sensorId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].NodeName.Should().Be("Alpha Node");
        result[1].NodeName.Should().Be("Test Node");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingAssignment_ReturnsAssignment()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(assignmentId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(assignmentId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingAssignment_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByEndpointAsync Tests

    [Fact]
    public async Task GetByEndpointAsync_WithExistingEndpoint_ReturnsAssignment()
    {
        // Arrange
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 5,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEndpointAsync(_nodeId, 5);

        // Assert
        result.Should().NotBeNull();
        result!.EndpointId.Should().Be(5);
    }

    [Fact]
    public async Task GetByEndpointAsync_WithNonExistingEndpoint_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByEndpointAsync(_nodeId, 99);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEndpointAsync_WithWrongNode_ReturnsNull()
    {
        // Arrange
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByEndpointAsync(Guid.NewGuid(), 1);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesAssignment()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1
        );

        // Act
        var result = await _sut.CreateAsync(_nodeId, dto);

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be(_nodeId);
        result.SensorId.Should().Be(_sensorId);
        result.EndpointId.Should().Be(1);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingNode_ThrowsException()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(Guid.NewGuid(), dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Node*not found*");
    }

    [Fact]
    public async Task CreateAsync_WithNonExistingSensor_ThrowsException()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: Guid.NewGuid(),
            EndpointId: 1
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(_nodeId, dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Sensor*not found*");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEndpointId_ThrowsException()
    {
        // Arrange
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1  // Already exists
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(_nodeId, dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*EndpointId*already exists*");
    }

    [Fact]
    public async Task CreateAsync_WithPinOverrides_SetsOverrides()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1,
            DigitalPinOverride: 17,
            IntervalSecondsOverride: 30
        );

        // Act
        var result = await _sut.CreateAsync(_nodeId, dto);

        // Assert
        result.Should().NotBeNull();

        // Verify persisted in database
        var persisted = await _context.NodeSensorAssignments.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.DigitalPinOverride.Should().Be(17);
        persisted.IntervalSecondsOverride.Should().Be(30);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingAssignment_UpdatesAssignment()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateNodeSensorAssignmentDto(
            IsActive: false,
            DigitalPinOverride: 22
        );

        // Act
        var result = await _sut.UpdateAsync(assignmentId, dto);

        // Assert
        result.Should().NotBeNull();
        result.IsActive.Should().BeFalse();

        // Verify persisted
        var persisted = await _context.NodeSensorAssignments.FindAsync(assignmentId);
        persisted!.IsActive.Should().BeFalse();
        persisted.DigitalPinOverride.Should().Be(22);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingAssignment_ThrowsException()
    {
        // Arrange
        var dto = new UpdateNodeSensorAssignmentDto(IsActive: false);

        // Act & Assert
        var act = () => _sut.UpdateAsync(Guid.NewGuid(), dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingAssignment_DeletesAssignment()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.DeleteAsync(assignmentId);

        // Assert
        var deleted = await _context.NodeSensorAssignments.FindAsync(assignmentId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingAssignment_ThrowsException()
    {
        // Act & Assert
        var act = () => _sut.DeleteAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    #endregion

    #region UpdateLastSeenAsync Tests

    [Fact]
    public async Task UpdateLastSeenAsync_WithExistingAssignment_UpdatesLastSeen()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var originalLastSeen = DateTime.UtcNow.AddHours(-1);
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = _nodeId,
            SensorId = _sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow,
            LastSeenAt = originalLastSeen
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.UpdateLastSeenAsync(assignmentId);

        // Assert
        var updated = await _context.NodeSensorAssignments.FindAsync(assignmentId);
        updated!.LastSeenAt.Should().BeAfter(originalLastSeen);
    }

    [Fact]
    public async Task UpdateLastSeenAsync_WithNonExistingAssignment_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.UpdateLastSeenAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    #endregion
}
