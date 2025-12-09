using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Repositories;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for HubService.
/// Single-Hub-Architecture: Only one Hub per Tenant/Installation allowed.
/// </summary>
public class HubServiceTests : IDisposable
{
    private readonly Infrastructure.Data.HubDbContext _context;
    private readonly HubService _sut;
    private readonly Mock<ILogger<HubService>> _loggerMock;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly ITenantService _tenantService;
    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public HubServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _loggerMock = new Mock<ILogger<HubService>>();
        _signalRMock = new Mock<ISignalRNotificationService>();
        var unitOfWork = new UnitOfWork(_context);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Hub:DefaultTenantId", _tenantId.ToString() }
            })
            .Build();

        _tenantService = new TenantService(
            _context, unitOfWork, Mock.Of<ILogger<TenantService>>(), config);

        _sut = new HubService(_context, unitOfWork, _tenantService, _signalRMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetCurrentHubAsync Tests (Single-Hub API)

    [Fact]
    public async Task GetCurrentHubAsync_WithExistingHub_ReturnsHub()
    {
        // Arrange
        _context.Hubs.Add(new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "my-iot-hub",
            Name = "My IoT Hub",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetCurrentHubAsync();

        // Assert
        result.Should().NotBeNull();
        result.HubId.Should().Be("my-iot-hub");
    }

    [Fact]
    public async Task GetCurrentHubAsync_WithNoHub_ThrowsException()
    {
        // Act & Assert
        var act = () => _sut.GetCurrentHubAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    #endregion

    #region UpdateCurrentHubAsync Tests (Single-Hub API)

    [Fact]
    public async Task UpdateCurrentHubAsync_WithExistingHub_UpdatesHub()
    {
        // Arrange
        _context.Hubs.Add(new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "my-iot-hub",
            Name = "Original Name",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateHubDto(Name: "Updated Name");

        // Act
        var result = await _sut.UpdateCurrentHubAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateCurrentHubAsync_WithNoHub_ThrowsException()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: "Test");

        // Act & Assert
        var act = () => _sut.UpdateCurrentHubAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    #endregion

    #region GetStatusAsync Tests (Single-Hub API)

    [Fact]
    public async Task GetStatusAsync_WithExistingHub_ReturnsStatus()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "my-iot-hub",
            Name = "My IoT Hub",
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsOnline.Should().BeTrue();
        result.NodeCount.Should().Be(0);
        result.OnlineNodeCount.Should().Be(0);
    }

    [Fact]
    public async Task GetStatusAsync_WithNoHub_ThrowsException()
    {
        // Act & Assert
        var act = () => _sut.GetStatusAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    #endregion

    #region EnsureDefaultHubAsync Tests (Single-Hub API)

    [Fact]
    public async Task EnsureDefaultHubAsync_WithNoHub_CreatesDefaultHub()
    {
        // Act
        await _sut.EnsureDefaultHubAsync();

        // Assert
        var hub = await _sut.GetCurrentHubAsync();
        hub.Should().NotBeNull();
        hub.HubId.Should().Be("my-iot-hub");
        hub.Name.Should().Be("My IoT Hub");
        hub.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureDefaultHubAsync_WithExistingHub_DoesNotCreateDuplicate()
    {
        // Arrange
        _context.Hubs.Add(new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "existing-hub",
            Name = "Existing Hub",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.EnsureDefaultHubAsync();

        // Assert
        var hub = await _sut.GetCurrentHubAsync();
        hub.HubId.Should().Be("existing-hub"); // Original hub unchanged
        _context.Hubs.Count().Should().Be(1);
    }

    #endregion

    #region Legacy API Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingHub_ReturnsHub()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "test-hub",
            Name = "Test Hub",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(hubId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Hub");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingHub_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithDifferentTenant_ReturnsNull()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = Guid.NewGuid(), // Different tenant
            HubId = "other-hub",
            Name = "Other Hub",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByIdAsync(hubId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByHubIdAsync_WithExistingHub_ReturnsHub()
    {
        // Arrange
        _context.Hubs.Add(new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "unique-hub-id",
            Name = "Unique Hub",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetByHubIdAsync("unique-hub-id");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Unique Hub");
    }

    [Fact]
    public async Task GetByHubIdAsync_WithNonExistingHub_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByHubIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOrCreateByHubIdAsync_WithExistingHub_UpdatesLastSeen()
    {
        // Arrange
        var existingHub = new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "existing-hub",
            Name = "Existing Hub",
            LastSeen = DateTime.UtcNow.AddHours(-1),
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Hubs.Add(existingHub);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetOrCreateByHubIdAsync("existing-hub");

        // Assert
        result.Should().NotBeNull();
        result.IsOnline.Should().BeTrue();
        result.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetOrCreateByHubIdAsync_WithNoHub_ThrowsException()
    {
        // Act & Assert (Single-Hub-Architecture: Hub must be initialized first)
        var act = () => _sut.GetOrCreateByHubIdAsync("new-hub-id");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingHub_UpdatesHub()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "update-hub",
            Name = "Original",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateHubDto(Name: "Updated Name");

        // Act
        var result = await _sut.UpdateAsync(hubId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingHub_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: "Test");

        // Act
        var result = await _sut.UpdateAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLastSeenAsync_UpdatesLastSeenAndOnlineStatus()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "lastseen-hub",
            Name = "LastSeen Hub",
            IsOnline = false,
            LastSeen = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.UpdateLastSeenAsync(hubId);

        // Assert
        var hub = await _sut.GetByIdAsync(hubId);
        hub!.IsOnline.Should().BeTrue();
        hub.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateLastSeenAsync_WithNonExistingHub_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.UpdateLastSeenAsync(Guid.NewGuid());
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetOnlineStatusAsync_WhenStatusChanges_NotifiesSignalR()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "online-hub",
            Name = "Online Hub",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetOnlineStatusAsync(hubId, false);

        // Assert
        _signalRMock.Verify(x => x.NotifyHubStatusChangedAsync(
            _tenantId,
            It.IsAny<HubDto>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetOnlineStatusAsync_WhenStatusUnchanged_DoesNotNotifySignalR()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "same-status-hub",
            Name = "Same Status Hub",
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        await _sut.SetOnlineStatusAsync(hubId, false);

        // Assert
        _signalRMock.Verify(x => x.NotifyHubStatusChangedAsync(
            It.IsAny<Guid>(),
            It.IsAny<HubDto>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetOnlineStatusAsync_WithNonExistingHub_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _sut.SetOnlineStatusAsync(Guid.NewGuid(), false);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetDefaultHubAsync_WithExistingHub_ReturnsHub()
    {
        // Arrange
        _context.Hubs.Add(new HubEntity
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "default-hub",
            Name = "Default Hub",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetDefaultHubAsync();

        // Assert
        result.Should().NotBeNull();
        result.HubId.Should().Be("default-hub");
    }

    [Fact]
    public async Task GetDefaultHubAsync_WithNoHub_CreatesDefaultHub()
    {
        // Act
        var result = await _sut.GetDefaultHubAsync();

        // Assert
        result.Should().NotBeNull();
        result.HubId.Should().Be("my-iot-hub");
        result.Name.Should().Be("My IoT Hub");
    }

    [Fact]
    public async Task GetStatusAsync_WithNodes_ReturnsCorrectCounts()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "hub-with-nodes",
            Name = "Hub With Nodes",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = "node-1",
            Name = "Online Node",
            IsOnline = true,
            CreatedAt = DateTime.UtcNow
        });

        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = "node-2",
            Name = "Offline Node",
            IsOnline = false,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetStatusAsync();

        // Assert
        result.NodeCount.Should().Be(2);
        result.OnlineNodeCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentHubAsync_IncludesNodes()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "hub-with-nodes",
            Name = "Hub With Nodes",
            CreatedAt = DateTime.UtcNow
        });

        _context.Nodes.Add(new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = "node-1",
            Name = "Test Node",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetCurrentHubAsync();

        // Assert
        result.SensorCount.Should().Be(1); // SensorCount is actually NodeCount in the mapping
    }

    [Fact]
    public async Task UpdateCurrentHubAsync_WithAllProperties_UpdatesAllFields()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new HubEntity
        {
            Id = hubId,
            TenantId = _tenantId,
            HubId = "original-hub",
            Name = "Original Name",
            Description = null,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new UpdateHubDto(Name: "Updated Name", Description: "New Description");

        // Act
        var result = await _sut.UpdateCurrentHubAsync(dto);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("New Description");
    }

    #endregion
}
