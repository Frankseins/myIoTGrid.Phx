using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Service.Services;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for NodeService (ESP32/LoRa32 Device management).
/// Matter-konform: Node entspricht einem Matter Node.
/// </summary>
public class NodeServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly NodeService _sut;
    private readonly Mock<ILogger<NodeService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId;

    public NodeServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<NodeService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        var unitOfWork = new UnitOfWork(_context);

        // Create a Hub
        _hubId = Guid.NewGuid();
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = _hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });

        // v3.0 Two-Tier: No SensorType setup needed, Sensor has Code/Name directly
        _context.SaveChanges();

        _sut = new NodeService(_context, unitOfWork, _signalRMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByHubAsync Tests

    [Fact]
    public async Task GetByHubAsync_WhenNoNodes_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetByHubAsync(_hubId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByHubAsync_ReturnsOnlyHubsNodes()
    {
        // Arrange
        var otherHubId = Guid.NewGuid();
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = otherHubId,
            TenantId = _tenantId,
            HubId = "other-hub",
            Name = "Other Hub",
            CreatedAt = DateTime.UtcNow
        });

        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "node-1",
            Name = "Node 1",
            CreatedAt = DateTime.UtcNow
        });
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = otherHubId, // Different Hub
            NodeId = "node-2",
            Name = "Node 2",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByHubAsync(_hubId);

        // Assert
        result.Should().ContainSingle();
        result.First().NodeId.Should().Be("node-1");
    }

    [Fact]
    public async Task GetByHubAsync_OrdersByName()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "z-node",
            Name = "Zebra Node",
            CreatedAt = DateTime.UtcNow
        });
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "a-node",
            Name = "Alpha Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByHubAsync(_hubId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Node");
        result[1].Name.Should().Be("Zebra Node");
    }

    [Fact]
    public async Task GetByHubAsync_IncludesAssignmentCount()
    {
        // Arrange - v3.0 Two-Tier: Node has SensorAssignments, Sensor has Code/Name directly
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "sensor-node",
            Name = "Sensor Node",
            CreatedAt = DateTime.UtcNow
        });

        // Create a Sensor with Code/Name directly
        var sensorId = Guid.NewGuid();
        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            TenantId = _tenantId,
            Code = "dht22-01",
            Name = "DHT22 Test Sensor",
            Protocol = CommunicationProtocol.OneWire,
            Category = "climate",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Create NodeSensorAssignment
        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            SensorId = sensorId,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetByHubAsync(_hubId)).First();

        // Assert
        result.AssignmentCount.Should().Be(1);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingNode_ReturnsNode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "test-node",
            Name = "Test Node",
            Protocol = Protocol.WLAN,
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Node");
        result.Protocol.Should().Be(ProtocolDto.WLAN);
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingNode_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetByNodeIdAsync Tests

    [Fact]
    public async Task GetByNodeIdAsync_WithExistingNode_ReturnsNode()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "wetterstation-01",
            Name = "Wetterstation 01",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNodeIdAsync(_hubId, "wetterstation-01");

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be("wetterstation-01");
    }

    [Fact]
    public async Task GetByNodeIdAsync_WithNonExistingNode_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByNodeIdAsync(_hubId, "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByNodeIdAsync_WithWrongHub_ReturnsNull()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "test-node",
            Name = "Test Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByNodeIdAsync(Guid.NewGuid(), "test-node");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetOrCreateByNodeIdAsync Tests

    [Fact]
    public async Task GetOrCreateByNodeIdAsync_WithExistingNode_UpdatesLastSeen()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var originalLastSeen = DateTime.UtcNow.AddHours(-1);
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "existing-node",
            Name = "Existing Node",
            LastSeen = originalLastSeen,
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetOrCreateByNodeIdAsync(_hubId, "existing-node");

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("existing-node");
        result.IsOnline.Should().BeTrue();
        result.LastSeen.Should().BeAfter(originalLastSeen);
    }

    [Fact]
    public async Task GetOrCreateByNodeIdAsync_WithNewNode_CreatesNode()
    {
        // Act
        var result = await _sut.GetOrCreateByNodeIdAsync(_hubId, "new-node");

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("new-node");
        result.Name.Should().Be("New Node"); // Generated from nodeId
        result.IsOnline.Should().BeTrue();
        result.Protocol.Should().Be(ProtocolDto.WLAN);

        // Verify persisted
        var persisted = await _context.Nodes.FindAsync(result.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrCreateByNodeIdAsync_GeneratesNameFromNodeId()
    {
        // Act
        var result = await _sut.GetOrCreateByNodeIdAsync(_hubId, "wetterstation-garten-01");

        // Assert
        result.Name.Should().Be("Wetterstation Garten 01");
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesNode()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "new-sensor",
            Name: "New Sensor",
            HubId: _hubId,
            Protocol: ProtocolDto.WLAN
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("new-sensor");
        result.Name.Should().Be("New Sensor");
        result.Protocol.Should().Be(ProtocolDto.WLAN);
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithLocation_CreatesNodeWithLocation()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "location-sensor",
            Name: "Location Sensor",
            HubId: _hubId,
            Location: new LocationDto("Wohnzimmer", 50.9375, 6.9603)
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Location.Should().NotBeNull();
        result.Location!.Name.Should().Be("Wohnzimmer");
        result.Location.Latitude.Should().Be(50.9375);
        result.Location.Longitude.Should().Be(6.9603);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNodeId_ThrowsException()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "duplicate-node",
            Name = "Duplicate Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateNodeDto(
            NodeId: "duplicate-node",
            HubId: _hubId
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateAsync_WithoutHubId_ThrowsException()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "orphan-node",
            HubId: null
        );

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HubId is required*");
    }

    [Fact]
    public async Task CreateAsync_WithoutName_GeneratesName()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "auto-name-node",
            HubId: _hubId
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Name.Should().Be("Auto Name Node");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithExistingNode_UpdatesNode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "update-node",
            Name = "Original Name",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateNodeDto(
            Name: "Updated Name",
            FirmwareVersion: "2.0.0"
        );

        // Act
        var result = await _sut.UpdateAsync(nodeId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");

        // Verify persisted
        var persisted = await _context.Nodes.FindAsync(nodeId);
        persisted!.Name.Should().Be("Updated Name");
        persisted.FirmwareVersion.Should().Be("2.0.0");
    }

    [Fact]
    public async Task UpdateAsync_WithLocation_UpdatesLocation()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "location-update",
            Name = "Location Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateNodeDto(
            Location: new LocationDto("Küche", null, null)
        );

        // Act
        var result = await _sut.UpdateAsync(nodeId, dto);

        // Assert
        result!.Location.Should().NotBeNull();
        result.Location!.Name.Should().Be("Küche");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingNode_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateNodeDto(Name: "Test");

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateLastSeenAsync Tests

    [Fact]
    public async Task UpdateLastSeenAsync_WithExistingNode_UpdatesTimestamp()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var originalLastSeen = DateTime.UtcNow.AddHours(-1);
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "lastseen-node",
            Name = "LastSeen Node",
            LastSeen = originalLastSeen,
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.UpdateLastSeenAsync(nodeId);

        // Assert
        var node = await _context.Nodes.FindAsync(nodeId);
        node!.LastSeen.Should().BeAfter(originalLastSeen);
        node.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLastSeenAsync_WithNonExistingNode_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.UpdateLastSeenAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region SetOnlineStatusAsync Tests

    [Fact]
    public async Task SetOnlineStatusAsync_SetsOnline()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "online-node",
            Name = "Online Node",
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetOnlineStatusAsync(nodeId, true);

        // Assert
        var node = await _context.Nodes.FindAsync(nodeId);
        node!.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task SetOnlineStatusAsync_SetsOffline()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "offline-node",
            Name = "Offline Node",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetOnlineStatusAsync(nodeId, false);

        // Assert
        var node = await _context.Nodes.FindAsync(nodeId);
        node!.IsOnline.Should().BeFalse();
    }

    [Fact]
    public async Task SetOnlineStatusAsync_WithNonExistingNode_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.SetOnlineStatusAsync(Guid.NewGuid(), true);
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Fact]
    public async Task UpdateStatusAsync_UpdatesAllStatusFields()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "status-node",
            Name = "Status Node",
            IsOnline = false,
            BatteryLevel = null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var lastSeen = DateTime.UtcNow;
        var status = new NodeStatusDto(
            NodeId: nodeId,
            IsOnline: true,
            LastSeen: lastSeen,
            BatteryLevel: 85
        );

        // Act
        await _sut.UpdateStatusAsync(nodeId, status);

        // Assert
        var node = await _context.Nodes.FindAsync(nodeId);
        node!.IsOnline.Should().BeTrue();
        node.LastSeen.Should().Be(lastSeen);
        node.BatteryLevel.Should().Be(85);
    }

    [Fact]
    public async Task UpdateStatusAsync_WithNullBatteryLevel_DoesNotUpdateBattery()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "battery-node",
            Name = "Battery Node",
            BatteryLevel = 50,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var status = new NodeStatusDto(
            NodeId: nodeId,
            IsOnline: true,
            LastSeen: null,
            BatteryLevel: null
        );

        // Act
        await _sut.UpdateStatusAsync(nodeId, status);

        // Assert
        var node = await _context.Nodes.FindAsync(nodeId);
        node!.BatteryLevel.Should().Be(50); // Unchanged
    }

    #endregion

    #region RegisterOrUpdateAsync Tests

    [Fact]
    public async Task RegisterOrUpdateAsync_WithNewNode_CreatesNode()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "register-new",
            Name: "Register New",
            HubId: _hubId
        );

        // Act
        var result = await _sut.RegisterOrUpdateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("register-new");

        var persisted = await _context.Nodes.FindAsync(result.Id);
        persisted.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterOrUpdateAsync_WithExistingNode_UpdatesNode()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var originalLastSeen = DateTime.UtcNow.AddHours(-1);
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "register-existing",
            Name = "Original Name",
            LastSeen = originalLastSeen,
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateNodeDto(
            NodeId: "register-existing",
            Name: "Updated Name",
            HubId: _hubId,
            Location: new LocationDto("Garten", null, null)
        );

        // Act
        var result = await _sut.RegisterOrUpdateAsync(dto);

        // Assert
        result.NodeId.Should().Be("register-existing");
        result.Name.Should().Be("Updated Name");
        result.IsOnline.Should().BeTrue();
        result.Location!.Name.Should().Be("Garten");
    }

    [Fact]
    public async Task RegisterOrUpdateAsync_WithoutHubId_ThrowsException()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "no-hub",
            HubId: null
        );

        // Act & Assert
        var act = () => _sut.RegisterOrUpdateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HubId is required*");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingNode_DeletesAndReturnsTrue()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "delete-node",
            Name = "Delete Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteAsync(nodeId);

        // Assert
        result.Should().BeTrue();
        var node = await _context.Nodes.FindAsync(nodeId);
        node.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingNode_ReturnsFalse()
    {
        // Act
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
