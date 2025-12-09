using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Helpers;
using myIoTGrid.Hub.Service.Services;

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
        _context.Hubs.Add(new HubEntity
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
        _context.Hubs.Add(new HubEntity
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

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenNoNodes_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllNodes()
    {
        // Arrange
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
            HubId = _hubId,
            NodeId = "node-2",
            Name = "Node 2",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "z-node",
            Name = "Zebra",
            CreatedAt = DateTime.UtcNow
        });
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "a-node",
            Name = "Alpha",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetAllAsync()).ToList();

        // Assert
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Zebra");
    }

    #endregion

    #region GetPagedAsync Tests

    [Fact]
    public async Task GetPagedAsync_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.Nodes.Add(new Node
            {
                Id = Guid.NewGuid(),
                HubId = _hubId,
                NodeId = $"node-{i:D2}",
                Name = $"Node {i:D2}",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Page = 1, Size = 5 };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().HaveCount(5);
        result.TotalRecords.Should().Be(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithSearchFilter_FiltersResults()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "temp-sensor",
            Name = "Temperature Sensor",
            CreatedAt = DateTime.UtcNow
        });
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "humidity-sensor",
            Name = "Humidity Sensor",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto { Search = "Temperature" };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Name.Should().Be("Temperature Sensor");
    }

    [Fact]
    public async Task GetPagedAsync_WithHubIdFilter_FiltersResults()
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
            HubId = otherHubId,
            NodeId = "node-2",
            Name = "Node 2",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "hubId", _hubId.ToString() } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Name.Should().Be("Node 1");
    }

    [Fact]
    public async Task GetPagedAsync_WithProtocolFilter_FiltersResults()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "wlan-node",
            Name = "WLAN Node",
            Protocol = Protocol.WLAN,
            CreatedAt = DateTime.UtcNow
        });
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "lora-node",
            Name = "LoRa Node",
            Protocol = Protocol.LoRaWAN,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "protocol", "WLAN" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().Name.Should().Be("WLAN Node");
    }

    [Fact]
    public async Task GetPagedAsync_WithIsOnlineFilter_FiltersResults()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "online-node",
            Name = "Online Node",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "offline-node",
            Name = "Offline Node",
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var queryParams = new QueryParamsDto
        {
            Filters = new Dictionary<string, string> { { "isOnline", "true" } }
        };

        // Act
        var result = await _sut.GetPagedAsync(queryParams);

        // Assert
        result.Items.Should().ContainSingle();
        result.Items.First().IsOnline.Should().BeTrue();
    }

    #endregion

    #region RegisterOrUpdateWithStatusAsync Tests

    [Fact]
    public async Task RegisterOrUpdateWithStatusAsync_WithNewNode_CreatesNodeAndReturnsIsNewTrue()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "new-status-node",
            Name: "New Status Node",
            HubId: _hubId
        );

        // Act
        var (node, isNew) = await _sut.RegisterOrUpdateWithStatusAsync(dto, "1.0.0");

        // Assert
        isNew.Should().BeTrue();
        node.NodeId.Should().Be("new-status-node");
    }

    [Fact]
    public async Task RegisterOrUpdateWithStatusAsync_WithExistingNode_UpdatesAndReturnsIsNewFalse()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "existing-status-node",
            Name = "Existing",
            FirmwareVersion = "1.0.0",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateNodeDto(
            NodeId: "existing-status-node",
            Name: "Updated",
            HubId: _hubId
        );

        // Act
        var (node, isNew) = await _sut.RegisterOrUpdateWithStatusAsync(dto, "2.0.0");

        // Assert
        isNew.Should().BeFalse();
        node.Name.Should().Be("Updated");

        // Verify firmware was updated
        var persisted = await _context.Nodes.FirstOrDefaultAsync(n => n.NodeId == "existing-status-node");
        persisted!.FirmwareVersion.Should().Be("2.0.0");
    }

    #endregion

    #region GetByMacAddressAsync Tests

    [Fact]
    public async Task GetByMacAddressAsync_WithExistingMac_ReturnsNode()
    {
        // Arrange
        var macAddress = "AA:BB:CC:DD:EE:FF";
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "mac-node",
            Name = "MAC Node",
            MacAddress = macAddress,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByMacAddressAsync(macAddress);

        // Assert
        result.Should().NotBeNull();
        result!.MacAddress.Should().Be(macAddress);
    }

    [Fact]
    public async Task GetByMacAddressAsync_WithNonExistingMac_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByMacAddressAsync("00:00:00:00:00:00");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByMacAddressAsync_WithLowerCaseMac_FindsNode()
    {
        // Arrange
        var macAddress = "AA:BB:CC:DD:EE:FF";
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "mac-node",
            Name = "MAC Node",
            MacAddress = macAddress,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - search with lowercase
        var result = await _sut.GetByMacAddressAsync("aa:bb:cc:dd:ee:ff");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region ProcessHeartbeatAsync Tests

    [Fact]
    public async Task ProcessHeartbeatAsync_WithExistingNode_UpdatesNodeAndReturnsSuccess()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "heartbeat-node",
            Name = "Heartbeat Node",
            IsOnline = false,
            BatteryLevel = 50,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new NodeHeartbeatDto(
            NodeId: "heartbeat-node",
            FirmwareVersion: "2.0.0",
            BatteryLevel: 75
        );

        // Act
        var result = await _sut.ProcessHeartbeatAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.NextHeartbeatSeconds.Should().Be(60);

        // Verify node was updated
        var node = await _context.Nodes.FirstOrDefaultAsync(n => n.NodeId == "heartbeat-node");
        node!.IsOnline.Should().BeTrue();
        node.FirmwareVersion.Should().Be("2.0.0");
        node.BatteryLevel.Should().Be(75);
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithUnknownNode_ReturnsFailure()
    {
        // Arrange
        var dto = new NodeHeartbeatDto(
            NodeId: "unknown-node"
        );

        // Act
        var result = await _sut.ProcessHeartbeatAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
    }

    #endregion

    #region RegisterNodeAsync Tests

    [Fact]
    public async Task RegisterNodeAsync_WithNewNode_CreatesNodeAndReturnsConfig()
    {
        // Arrange
        var dto = new NodeRegistrationDto(
            MacAddress: "11:22:33:44:55:66",
            FirmwareVersion: "1.0.0"
        );

        // Act
        var result = await _sut.RegisterNodeAsync(dto, "MyWiFi", "password123", "http://hub.local:5000");

        // Assert
        result.Should().NotBeNull();
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.WifiSsid.Should().Be("MyWiFi");
        result.WifiPassword.Should().Be("password123");
        result.HubApiUrl.Should().Be("http://hub.local:5000");

        // Verify node was created
        var node = await _context.Nodes.FirstOrDefaultAsync(n => n.MacAddress == "11:22:33:44:55:66");
        node.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterNodeAsync_WithExistingMac_RegeneratesApiKey()
    {
        // Arrange
        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "existing-mac-node",
            Name = "Existing MAC Node",
            MacAddress = "AA:AA:AA:AA:AA:AA",
            ApiKeyHash = "old-hash",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new NodeRegistrationDto(
            MacAddress: "AA:AA:AA:AA:AA:AA",
            FirmwareVersion: "1.0.0"
        );

        // Act
        var result = await _sut.RegisterNodeAsync(dto, "WiFi", "pass", "http://hub.local");

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("existing-mac-node");
    }

    [Fact]
    public async Task RegisterNodeAsync_WithNoHub_ThrowsException()
    {
        // Arrange - remove all hubs
        _context.Hubs.RemoveRange(_context.Hubs);
        await _context.SaveChangesAsync();

        var dto = new NodeRegistrationDto(
            MacAddress: "BB:BB:BB:BB:BB:BB",
            FirmwareVersion: "1.0.0"
        );

        // Act & Assert
        var act = () => _sut.RegisterNodeAsync(dto, "WiFi", "pass", "http://hub.local");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No Hub configured*");
    }

    #endregion

    #region RegenerateApiKeyAsync Tests

    [Fact]
    public async Task RegenerateApiKeyAsync_WithExistingNode_RegeneratesKey()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "regen-node",
            Name = "Regen Node",
            ApiKeyHash = "old-hash",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.RegenerateApiKeyAsync(nodeId, "WiFi", "pass", "http://hub.local");

        // Assert
        result.Should().NotBeNull();
        result!.ApiKey.Should().NotBeNullOrEmpty();
        result.NodeId.Should().Be("regen-node");
    }

    [Fact]
    public async Task RegenerateApiKeyAsync_WithNonExistingNode_ReturnsNull()
    {
        // Act
        var result = await _sut.RegenerateApiKeyAsync(Guid.NewGuid(), "WiFi", "pass", "http://hub.local");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSensorsLatestAsync Tests (US-8.5.1)

    [Fact]
    public async Task GetSensorsLatestAsync_WithNonExistingNode_ReturnsNull()
    {
        // Act
        var result = await _sut.GetSensorsLatestAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithNodeWithoutAssignments_ReturnsEmptySensorsList()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "empty-node",
            Name = "Empty Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.NodeName.Should().Be("Empty Node");
        result.Sensors.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithSensorAndReadings_ReturnsLatestReading()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "sensor-node",
            Name = "Sensor Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            TenantId = _tenantId,
            Code = "ds18b20",
            Name = "DS18B20",
            Model = "Dallas DS18B20",
            Protocol = CommunicationProtocol.OneWire,
            Category = "temperature",
            Icon = "thermostat",
            Color = "#FF5722",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = nodeId,
            SensorId = sensorId,
            EndpointId = 1,
            Alias = "Pool Temperatur Oben",
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        // Add readings - older first
        _context.Readings.Add(new Reading
        {
            Id = 1,
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            RawValue = 24.5,
            Value = 24.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow.AddMinutes(-10)
        });

        // Add newer reading
        _context.Readings.Add(new Reading
        {
            Id = 2,
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignmentId,
            MeasurementType = "temperature",
            RawValue = 25.3,
            Value = 25.3,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.NodeName.Should().Be("Sensor Node");
        result.Sensors.Should().ContainSingle();

        var sensor = result.Sensors.First();
        sensor.DisplayName.Should().Be("Pool Temperatur Oben"); // Alias takes priority
        sensor.FullName.Should().Be("DS18B20"); // Uses Sensor.Name
        sensor.SensorCode.Should().Be("ds18b20");
        sensor.SensorModel.Should().Be("Dallas DS18B20");
        sensor.Icon.Should().Be("thermostat");
        sensor.Color.Should().Be("#FF5722");

        sensor.Measurements.Should().ContainSingle();
        var measurement = sensor.Measurements.First();
        measurement.MeasurementType.Should().Be("temperature");
        measurement.Value.Should().Be(25.3); // Latest value
        measurement.Unit.Should().Be("°C");
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithMultipleSensors_ReturnsAllSensorsWithLatestReadings()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensor1Id = Guid.NewGuid();
        var sensor2Id = Guid.NewGuid();
        var assignment1Id = Guid.NewGuid();
        var assignment2Id = Guid.NewGuid();

        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "multi-sensor-node",
            Name = "Multi Sensor Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Sensors.Add(new Sensor
        {
            Id = sensor1Id,
            TenantId = _tenantId,
            Code = "ds18b20",
            Name = "DS18B20",
            Model = "Dallas DS18B20",
            Protocol = CommunicationProtocol.OneWire,
            Category = "temperature",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.Sensors.Add(new Sensor
        {
            Id = sensor2Id,
            TenantId = _tenantId,
            Code = "bme280",
            Name = "BME280",
            Model = "Bosch BME280",
            Protocol = CommunicationProtocol.I2C,
            Category = "climate",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignment1Id,
            NodeId = nodeId,
            SensorId = sensor1Id,
            EndpointId = 1,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignment2Id,
            NodeId = nodeId,
            SensorId = sensor2Id,
            EndpointId = 2,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        // Readings for sensor 1
        _context.Readings.Add(new Reading
        {
            Id = 1,
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignment1Id,
            MeasurementType = "temperature",
            RawValue = 22.5,
            Value = 22.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });

        // Readings for sensor 2 (multiple measurement types)
        _context.Readings.Add(new Reading
        {
            Id = 2,
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignment2Id,
            MeasurementType = "temperature",
            RawValue = 23.1,
            Value = 23.1,
            Unit = "°C",
            Timestamp = DateTime.UtcNow
        });

        _context.Readings.Add(new Reading
        {
            Id = 3,
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignment2Id,
            MeasurementType = "humidity",
            RawValue = 65.2,
            Value = 65.2,
            Unit = "%",
            Timestamp = DateTime.UtcNow
        });

        _context.Readings.Add(new Reading
        {
            Id = 4,
            TenantId = _tenantId,
            NodeId = nodeId,
            AssignmentId = assignment2Id,
            MeasurementType = "pressure",
            RawValue = 1013.25,
            Value = 1013.25,
            Unit = "hPa",
            Timestamp = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.Sensors.Should().HaveCount(2);

        // DS18B20 sensor
        var ds18b20 = result.Sensors.FirstOrDefault(s => s.SensorCode == "ds18b20");
        ds18b20.Should().NotBeNull();
        ds18b20!.DisplayName.Should().Be("DS18B20"); // No alias, uses sensor name
        ds18b20.Measurements.Should().ContainSingle();

        // BME280 sensor - should have 3 measurement types
        var bme280 = result.Sensors.FirstOrDefault(s => s.SensorCode == "bme280");
        bme280.Should().NotBeNull();
        bme280!.DisplayName.Should().Be("BME280");
        bme280.Measurements.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSensorsLatestAsync_DisplayNamePriority_UsesAliasOverSensorName()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "alias-node",
            Name = "Alias Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            TenantId = _tenantId,
            Code = "ds18b20",
            Name = "DS18B20 Temperature Sensor",
            Model = "Dallas DS18B20",
            Protocol = CommunicationProtocol.OneWire,
            Category = "temperature",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = nodeId,
            SensorId = sensorId,
            EndpointId = 1,
            Alias = "Außentemperatur", // Alias should take priority
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.Sensors.Should().ContainSingle();
        var sensor = result.Sensors.First();
        sensor.DisplayName.Should().Be("Außentemperatur");
        sensor.Alias.Should().Be("Außentemperatur");
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithInactiveSensor_ExcludesInactiveAssignments()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var sensorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();

        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "inactive-sensor-node",
            Name = "Inactive Sensor Node",
            CreatedAt = DateTime.UtcNow
        });

        _context.Sensors.Add(new Sensor
        {
            Id = sensorId,
            TenantId = _tenantId,
            Code = "ds18b20",
            Name = "DS18B20",
            Model = "Dallas DS18B20",
            Protocol = CommunicationProtocol.OneWire,
            Category = "temperature",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _context.NodeSensorAssignments.Add(new NodeSensorAssignment
        {
            Id = assignmentId,
            NodeId = nodeId,
            SensorId = sensorId,
            EndpointId = 1,
            IsActive = false, // Inactive assignment - should be excluded
            AssignedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert - inactive assignments are filtered out
        result.Should().NotBeNull();
        result!.Sensors.Should().BeEmpty(); // No active assignments
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithNodeLocation_IncludesLocationName()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "location-node",
            Name = "Location Node",
            Location = new Location { Name = "Wohnzimmer" },
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.LocationName.Should().Be("Wohnzimmer");
    }

    #endregion

    #region Node Provisioning Tests

    [Fact]
    public async Task GetByMacAddressAsync_WithExistingNode_ReturnsNode()
    {
        // Arrange
        var macAddress = "AA:BB:CC:DD:EE:FF";
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "mac-node",
            Name = "MAC Node",
            MacAddress = macAddress.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByMacAddressAsync(macAddress);

        // Assert
        result.Should().NotBeNull();
        result!.MacAddress.Should().Be(macAddress.ToUpperInvariant());
    }

    [Fact]
    public async Task GetByMacAddressAsync_WithLowerCaseMac_NormalizesToUpperCase()
    {
        // Arrange
        var macAddress = "aa:bb:cc:dd:ee:ff";
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "lower-mac-node",
            Name = "Lower MAC Node",
            MacAddress = macAddress.ToUpperInvariant(),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act - query with lowercase
        var result = await _sut.GetByMacAddressAsync(macAddress);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithExistingNode_UpdatesStatusAndReturnsSuccess()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "heartbeat-node",
            Name = "Heartbeat Node",
            IsOnline = false,
            LastSeen = DateTime.UtcNow.AddHours(-1),
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        var dto = new NodeHeartbeatDto("heartbeat-node", "1.2.0", 85);

        // Act
        var result = await _sut.ProcessHeartbeatAsync(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.NextHeartbeatSeconds.Should().Be(60);

        // Verify node was updated
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.IsOnline.Should().BeTrue();
        updatedNode.FirmwareVersion.Should().Be("1.2.0");
        updatedNode.BatteryLevel.Should().Be(85);
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithUnknownNode_ReturnsFalse()
    {
        // Arrange
        var dto = new NodeHeartbeatDto("unknown-node", null, null);

        // Act
        var result = await _sut.ProcessHeartbeatAsync(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.NextHeartbeatSeconds.Should().Be(60);
    }

    [Fact]
    public async Task ProcessHeartbeatAsync_WithoutOptionalFields_UpdatesOnlyRequiredFields()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _context.Nodes.Add(new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "minimal-heartbeat-node",
            Name = "Minimal Heartbeat Node",
            FirmwareVersion = "1.0.0",
            BatteryLevel = 100,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new NodeHeartbeatDto("minimal-heartbeat-node", null, null);

        // Act
        var result = await _sut.ProcessHeartbeatAsync(dto);

        // Assert
        result.Success.Should().BeTrue();

        // Verify original values preserved
        var updatedNode = await _context.Nodes.FindAsync(nodeId);
        updatedNode!.FirmwareVersion.Should().Be("1.0.0"); // Unchanged
        updatedNode.BatteryLevel.Should().Be(100); // Unchanged
        updatedNode.IsOnline.Should().BeTrue(); // Updated
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithInvalidFormat_ReturnsNull()
    {
        // Act - Invalid format (not starting with mig_)
        var result = await _sut.ValidateApiKeyAsync("any-node", "invalid-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithNonExistingNode_ReturnsNull()
    {
        // Act
        var result = await _sut.ValidateApiKeyAsync("nonexistent-node", "mig_validformat123456789012345");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithWrongApiKey_ReturnsNull()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var wrongApiKey = ApiKeyGenerator.GenerateApiKey(); // Different key
        var node = new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "api-key-test-node",
            Name = "API Key Test Node",
            ApiKeyHash = ApiKeyGenerator.HashApiKey(apiKey), // Hashed with correct key
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act - Use wrong key
        var result = await _sut.ValidateApiKeyAsync("api-key-test-node", wrongApiKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithValidApiKey_ReturnsNode()
    {
        // Arrange
        var apiKey = ApiKeyGenerator.GenerateApiKey();
        var node = new Node
        {
            Id = Guid.NewGuid(),
            HubId = _hubId,
            NodeId = "valid-api-key-node",
            Name = "Valid API Key Node",
            ApiKeyHash = ApiKeyGenerator.HashApiKey(apiKey),
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ValidateApiKeyAsync("valid-api-key-node", apiKey);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be("valid-api-key-node");
        result.Name.Should().Be("Valid API Key Node");
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithReadingsButNoCapabilities_UsesFormattedMeasurementType()
    {
        // Arrange - Create a node for this test
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "basic-sensor-node",
            Name = "Basic Sensor Node",
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);

        // Create sensor with no capabilities
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Code = "BASIC",
            Name = "Basic Sensor",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(sensor);

        var assignment = new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            SensorId = sensor.Id,
            EndpointId = 10,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };
        _context.NodeSensorAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        // Add reading without capability match
        var reading = new Reading
        {
            NodeId = nodeId,
            AssignmentId = assignment.Id,
            MeasurementType = "unknown_type", // No mapping
            RawValue = 42.0,
            Value = 42.0,
            Unit = "units",
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false
        };
        _context.Readings.Add(reading);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        // Should format "unknown_type" to "Unknown_type" using FormatMeasurementType
    }

    [Fact]
    public async Task GetSensorsLatestAsync_WithMappedMeasurementTypes_ReturnsMappedNames()
    {
        // Arrange - Create a node for this test
        var nodeId = Guid.NewGuid();
        var node = new Node
        {
            Id = nodeId,
            HubId = _hubId,
            NodeId = "mapped-sensor-node",
            Name = "Mapped Sensor Node",
            CreatedAt = DateTime.UtcNow
        };
        _context.Nodes.Add(node);

        // Add sensor with known measurement type
        var sensor = new Sensor
        {
            Id = Guid.NewGuid(),
            Code = "TEMP",
            Name = "Temp Sensor",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Sensors.Add(sensor);

        var assignment = new NodeSensorAssignment
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            SensorId = sensor.Id,
            EndpointId = 11,
            IsActive = true,
            AssignedAt = DateTime.UtcNow
        };
        _context.NodeSensorAssignments.Add(assignment);

        // Add capability for sensor
        var capability = new SensorCapability
        {
            Id = Guid.NewGuid(),
            SensorId = sensor.Id,
            MeasurementType = "temperature",
            DisplayName = "Temperatur",
            Unit = "°C",
            SortOrder = 1,
            IsActive = true
        };
        _context.SensorCapabilities.Add(capability);
        await _context.SaveChangesAsync();

        // Add reading
        var reading = new Reading
        {
            NodeId = nodeId,
            AssignmentId = assignment.Id,
            MeasurementType = "temperature",
            RawValue = 21.5,
            Value = 21.5,
            Unit = "°C",
            Timestamp = DateTime.UtcNow,
            IsSyncedToCloud = false
        };
        _context.Readings.Add(reading);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetSensorsLatestAsync(nodeId);

        // Assert
        result.Should().NotBeNull();
        var sensorReading = result!.Sensors.FirstOrDefault(s => s.SensorCode == "TEMP");
        sensorReading.Should().NotBeNull();
        sensorReading!.Measurements.Should().ContainSingle(m => m.DisplayName == "Temperatur");
    }

    #endregion
}
