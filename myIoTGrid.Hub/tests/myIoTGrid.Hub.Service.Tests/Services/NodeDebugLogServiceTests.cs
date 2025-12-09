using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Services;

namespace myIoTGrid.Hub.Service.Tests.Services;

/// <summary>
/// Tests for NodeDebugLogService (Sprint 8: Remote Debug System).
/// </summary>
public class NodeDebugLogServiceTests : IDisposable
{
    private readonly HubDbContext _context;
    private readonly Mock<ISignalRNotificationService> _signalRMock;
    private readonly Mock<ILogger<NodeDebugLogService>> _loggerMock;
    private readonly NodeDebugLogService _sut;

    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _hubId = Guid.NewGuid();
    private readonly Guid _nodeId = Guid.NewGuid();
    private readonly string _macAddress = "AA:BB:CC:DD:EE:FF";

    public NodeDebugLogServiceTests()
    {
        var options = new DbContextOptionsBuilder<HubDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new HubDbContext(options);
        _signalRMock = new Mock<ISignalRNotificationService>();
        _loggerMock = new Mock<ILogger<NodeDebugLogService>>();

        _sut = new NodeDebugLogService(_context, _signalRMock.Object, _loggerMock.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var tenant = new Tenant { Id = _tenantId, Name = "Test Tenant" };
        var hub = new HubEntity { Id = _hubId, TenantId = _tenantId, HubId = "test-hub", Name = "Test Hub" };
        var node = new Node
        {
            Id = _nodeId,
            HubId = _hubId,
            NodeId = "test-node",
            Name = "Test Node",
            MacAddress = _macAddress,
            DebugLevel = DebugLevel.Normal,
            EnableRemoteLogging = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tenants.Add(tenant);
        _context.Hubs.Add(hub);
        _context.Nodes.Add(node);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region GetLogsAsync Tests

    [Fact]
    public async Task GetLogsAsync_ReturnsLogs_ForNode()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Log 1", ReceivedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Debug, Category = LogCategory.Sensor, Message = "Log 2", ReceivedAt = DateTime.UtcNow.AddMinutes(-3) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Production, Category = LogCategory.Error, Message = "Error Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        var filter = new DebugLogFilterDto(_nodeId, null, null, null, null, 1, 10);

        // Act
        var result = await _sut.GetLogsAsync(filter);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetLogsAsync_FiltersByMinLevel()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Production, Category = LogCategory.System, Message = "Production Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Normal Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-3) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Debug, Category = LogCategory.System, Message = "Debug Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        var filter = new DebugLogFilterDto(_nodeId, DebugLevelDto.Normal, null, null, null, 1, 10);

        // Act
        var result = await _sut.GetLogsAsync(filter);

        // Assert
        result.Items.Should().HaveCount(2); // Normal and Debug (>= Normal)
    }

    [Fact]
    public async Task GetLogsAsync_FiltersByCategory()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "System Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.Sensor, Message = "Sensor Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-3) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.Error, Message = "Error Log", ReceivedAt = DateTime.UtcNow.AddMinutes(-1) }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        var filter = new DebugLogFilterDto(_nodeId, null, LogCategoryDto.Sensor, null, null, 1, 10);

        // Act
        var result = await _sut.GetLogsAsync(filter);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Category.Should().Be(LogCategoryDto.Sensor);
    }

    [Fact]
    public async Task GetLogsAsync_FiltersByDateRange()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Old Log", ReceivedAt = now.AddHours(-5) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Recent Log", ReceivedAt = now.AddMinutes(-30) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "New Log", ReceivedAt = now.AddMinutes(-10) }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        var filter = new DebugLogFilterDto(_nodeId, null, null, now.AddHours(-1), now, 1, 10);

        // Act
        var result = await _sut.GetLogsAsync(filter);

        // Assert
        result.Items.Should().HaveCount(2); // Only logs within last hour
    }

    [Fact]
    public async Task GetLogsAsync_AppliesPagination()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            _context.NodeDebugLogs.Add(new NodeDebugLog
            {
                Id = Guid.NewGuid(),
                NodeId = _nodeId,
                Level = DebugLevel.Normal,
                Category = LogCategory.System,
                Message = $"Log {i}",
                ReceivedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        var filter = new DebugLogFilterDto(_nodeId, null, null, null, null, 2, 10);

        // Act
        var result = await _sut.GetLogsAsync(filter);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.Page.Should().Be(2);
    }

    #endregion

    #region GetRecentLogsAsync Tests

    [Fact]
    public async Task GetRecentLogsAsync_ReturnsRecentLogs()
    {
        // Arrange
        for (int i = 0; i < 60; i++)
        {
            _context.NodeDebugLogs.Add(new NodeDebugLog
            {
                Id = Guid.NewGuid(),
                NodeId = _nodeId,
                Level = DebugLevel.Normal,
                Category = LogCategory.System,
                Message = $"Log {i}",
                ReceivedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetRecentLogsAsync(_nodeId, 50);

        // Assert
        result.Should().HaveCount(50);
    }

    [Fact]
    public async Task GetRecentLogsAsync_ReturnsLogsOrderedByTimestamp()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Oldest", ReceivedAt = DateTime.UtcNow.AddHours(-2) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Newest", ReceivedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Middle", ReceivedAt = DateTime.UtcNow.AddHours(-1) }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _sut.GetRecentLogsAsync(_nodeId)).ToList();

        // Assert
        result.First().Message.Should().Be("Newest");
        result.Last().Message.Should().Be("Oldest");
    }

    #endregion

    #region CreateBatchAsync Tests

    [Fact]
    public async Task CreateBatchAsync_CreatesLogs_WhenNodeExistsAndLoggingEnabled()
    {
        // Arrange
        var logs = new List<CreateNodeDebugLogDto>
        {
            new() { NodeTimestamp = 1000, Level = DebugLevelDto.Normal, Category = LogCategoryDto.System, Message = "Test Log 1" },
            new() { NodeTimestamp = 2000, Level = DebugLevelDto.Debug, Category = LogCategoryDto.Sensor, Message = "Test Log 2" }
        };

        // Act
        var result = await _sut.CreateBatchAsync(_macAddress, logs);

        // Assert
        result.Should().Be(2);
        var savedLogs = await _context.NodeDebugLogs.Where(l => l.NodeId == _nodeId).ToListAsync();
        savedLogs.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateBatchAsync_Returns0_WhenNodeNotFound()
    {
        // Arrange
        var logs = new List<CreateNodeDebugLogDto>
        {
            new() { NodeTimestamp = 1000, Level = DebugLevelDto.Normal, Category = LogCategoryDto.System, Message = "Test Log" }
        };

        // Act
        var result = await _sut.CreateBatchAsync("XX:XX:XX:XX:XX:XX", logs);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CreateBatchAsync_Returns0_WhenRemoteLoggingDisabled()
    {
        // Arrange
        var node = await _context.Nodes.FindAsync(_nodeId);
        node!.EnableRemoteLogging = false;
        await _context.SaveChangesAsync();

        var logs = new List<CreateNodeDebugLogDto>
        {
            new() { NodeTimestamp = 1000, Level = DebugLevelDto.Normal, Category = LogCategoryDto.System, Message = "Test Log" }
        };

        // Act
        var result = await _sut.CreateBatchAsync(_macAddress, logs);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CreateBatchAsync_NotifiesSignalR()
    {
        // Arrange
        var logs = new List<CreateNodeDebugLogDto>
        {
            new() { NodeTimestamp = 1000, Level = DebugLevelDto.Normal, Category = LogCategoryDto.System, Message = "Test Log" }
        };

        // Act
        await _sut.CreateBatchAsync(_macAddress, logs);

        // Assert
        _signalRMock.Verify(
            x => x.NotifyDebugLogReceivedAsync(It.IsAny<NodeDebugLogDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetDebugConfigurationAsync Tests

    [Fact]
    public async Task GetDebugConfigurationAsync_ReturnsConfig_WhenNodeExists()
    {
        // Act
        var result = await _sut.GetDebugConfigurationAsync(_nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(_nodeId);
        result.EnableRemoteLogging.Should().BeTrue();
    }

    [Fact]
    public async Task GetDebugConfigurationAsync_ReturnsNull_WhenNodeNotFound()
    {
        // Act
        var result = await _sut.GetDebugConfigurationAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDebugConfigurationBySerialAsync_ReturnsConfig()
    {
        // Act
        var result = await _sut.GetDebugConfigurationBySerialAsync(_macAddress);

        // Assert
        result.Should().NotBeNull();
        result!.SerialNumber.Should().Be(_macAddress);
    }

    #endregion

    #region SetDebugLevelAsync Tests

    [Fact]
    public async Task SetDebugLevelAsync_UpdatesNode()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Debug, true);

        // Act
        var result = await _sut.SetDebugLevelAsync(_nodeId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.DebugLevel.Should().Be(DebugLevelDto.Debug);
        result.EnableRemoteLogging.Should().BeTrue();

        var node = await _context.Nodes.FindAsync(_nodeId);
        node!.DebugLevel.Should().Be(DebugLevel.Debug);
        node.LastDebugChange.Should().NotBeNull();
    }

    [Fact]
    public async Task SetDebugLevelAsync_ReturnsNull_WhenNodeNotFound()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Debug, true);

        // Act
        var result = await _sut.SetDebugLevelAsync(Guid.NewGuid(), dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SetDebugLevelAsync_NotifiesSignalR()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Debug, true);

        // Act
        await _sut.SetDebugLevelAsync(_nodeId, dto);

        // Assert
        _signalRMock.Verify(
            x => x.NotifyDebugConfigChangedAsync(It.IsAny<NodeDebugConfigurationDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetDebugLevelBySerialAsync_UpdatesNode()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Production, false);

        // Act
        var result = await _sut.SetDebugLevelBySerialAsync(_macAddress, dto);

        // Assert
        result.Should().NotBeNull();
        result!.DebugLevel.Should().Be(DebugLevelDto.Production);
    }

    #endregion

    #region GetErrorStatisticsAsync Tests

    [Fact]
    public async Task GetErrorStatisticsAsync_ReturnsStats()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.Error, Message = "Error 1", ReceivedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.Error, Message = "Error 2", ReceivedAt = DateTime.UtcNow.AddMinutes(-5) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Debug, Category = LogCategory.System, Message = "Info", ReceivedAt = DateTime.UtcNow }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetErrorStatisticsAsync(_nodeId);

        // Assert
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(_nodeId);
        result.TotalLogs.Should().Be(3);
        result.ErrorCount.Should().Be(2);
    }

    [Fact]
    public async Task GetErrorStatisticsAsync_ReturnsNull_WhenNodeNotFound()
    {
        // Act
        var result = await _sut.GetErrorStatisticsAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllErrorStatisticsAsync_ReturnsStatsForAllNodes()
    {
        // Act
        var result = await _sut.GetAllErrorStatisticsAsync();

        // Assert
        result.Should().NotBeEmpty();
    }

    #endregion

    #region CleanupLogsAsync Tests

    [Fact]
    public async Task CleanupLogsAsync_DeletesOldLogs()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Old Log", ReceivedAt = DateTime.UtcNow.AddDays(-10) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Recent Log", ReceivedAt = DateTime.UtcNow }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.CleanupLogsAsync(DateTime.UtcNow.AddDays(-5));

        // Assert
        result.DeletedCount.Should().Be(1);
        var remainingLogs = await _context.NodeDebugLogs.ToListAsync();
        remainingLogs.Should().HaveCount(1);
    }

    #endregion

    #region ClearLogsAsync Tests

    [Fact]
    public async Task ClearLogsAsync_DeletesAllLogsForNode()
    {
        // Arrange
        var logs = new List<NodeDebugLog>
        {
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Log 1", ReceivedAt = DateTime.UtcNow.AddMinutes(-10) },
            new() { Id = Guid.NewGuid(), NodeId = _nodeId, Level = DebugLevel.Normal, Category = LogCategory.System, Message = "Log 2", ReceivedAt = DateTime.UtcNow }
        };
        _context.NodeDebugLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.ClearLogsAsync(_nodeId);

        // Assert
        result.Should().Be(2);
        var remainingLogs = await _context.NodeDebugLogs.Where(l => l.NodeId == _nodeId).ToListAsync();
        remainingLogs.Should().BeEmpty();
    }

    #endregion
}
