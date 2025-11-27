using FluentAssertions;
using Microsoft.Extensions.Configuration;
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

    [Fact]
    public async Task GetAllAsync_WhenNoHubs_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyTenantsHubs()
    {
        // Arrange
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "hub-1",
            Name = "Hub 1",
            CreatedAt = DateTime.UtcNow
        });
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(), // Different tenant
            HubId = "hub-2",
            Name = "Hub 2",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().HubId.Should().Be("hub-1");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingHub_ReturnsHub()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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
        var existingHub = new myIoTGrid.Hub.Domain.Entities.Hub
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
    public async Task GetOrCreateByHubIdAsync_WithNewHub_CreatesHub()
    {
        // Act
        var result = await _sut.GetOrCreateByHubIdAsync("new-hub-id");

        // Assert
        result.Should().NotBeNull();
        result.HubId.Should().Be("new-hub-id");
        result.Name.Should().Be("New Hub Id");
        result.IsOnline.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_CreatesHub()
    {
        // Arrange
        var dto = new CreateHubDto(
            HubId: "created-hub",
            Name: "Created Hub"
        );

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.HubId.Should().Be("created-hub");
        result.Name.Should().Be("Created Hub");
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateHubId_ThrowsException()
    {
        // Arrange
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            HubId = "duplicate-hub",
            Name = "Duplicate",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var dto = new CreateHubDto(HubId: "duplicate-hub", Name: "New Hub");

        // Act & Assert
        var act = () => _sut.CreateAsync(dto);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task UpdateAsync_WithExistingHub_UpdatesHub()
    {
        // Arrange
        var hubId = Guid.NewGuid();
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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
        _context.Hubs.Add(new myIoTGrid.Hub.Domain.Entities.Hub
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

    [Theory]
    [InlineData("hub-home-01", "Hub Home 01")]
    [InlineData("sensor_wohnzimmer_temp", "Sensor Wohnzimmer Temp")]
    [InlineData("simple", "Simple")]
    [InlineData("", "Unknown Hub")]
    public async Task GetOrCreateByHubIdAsync_GeneratesCorrectName(string hubId, string expectedName)
    {
        // Act
        var result = await _sut.GetOrCreateByHubIdAsync(hubId);

        // Assert
        if (string.IsNullOrEmpty(hubId))
        {
            result.Name.Should().Be("Unknown Hub");
        }
        else
        {
            result.Name.Should().Be(expectedName);
        }
    }
}
