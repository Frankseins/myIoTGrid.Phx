using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Interface.Hubs;

namespace myIoTGrid.Hub.Interface.Tests.Hubs;

/// <summary>
/// Unit tests for SensorHub SignalR Hub
/// </summary>
public class SensorHubTests
{
    private readonly Mock<ITenantService> _tenantServiceMock;
    private readonly Mock<ILogger<SensorHub>> _loggerMock;
    private readonly Mock<IGroupManager> _groupManagerMock;
    private readonly Mock<HubCallerContext> _contextMock;
    private readonly SensorHub _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly string _connectionId = "test-connection-id";

    public SensorHubTests()
    {
        _tenantServiceMock = new Mock<ITenantService>();
        _loggerMock = new Mock<ILogger<SensorHub>>();
        _groupManagerMock = new Mock<IGroupManager>();
        _contextMock = new Mock<HubCallerContext>();

        _tenantServiceMock.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);
        _contextMock.Setup(c => c.ConnectionId).Returns(_connectionId);

        _sut = new SensorHub(_tenantServiceMock.Object, _loggerMock.Object);

        // Use reflection to set the protected properties
        var groupsProperty = typeof(Microsoft.AspNetCore.SignalR.Hub).GetProperty("Groups");
        var contextProperty = typeof(Microsoft.AspNetCore.SignalR.Hub).GetProperty("Context");
        groupsProperty!.SetValue(_sut, _groupManagerMock.Object);
        contextProperty!.SetValue(_sut, _contextMock.Object);
    }

    #region OnConnectedAsync Tests

    [Fact]
    public async Task OnConnectedAsync_AddsClientToTenantGroup()
    {
        // Act
        await _sut.OnConnectedAsync();

        // Assert
        var expectedGroupName = $"tenant:{_tenantId}";
        _groupManagerMock.Verify(g => g.AddToGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region OnDisconnectedAsync Tests

    [Fact]
    public async Task OnDisconnectedAsync_WithNoException_CompletesSuccessfully()
    {
        // Act
        await _sut.OnDisconnectedAsync(null);

        // Assert - should not throw
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_LogsWarning()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        await _sut.OnDisconnectedAsync(exception);

        // Assert - should not throw
    }

    #endregion

    #region JoinHubGroup Tests

    [Fact]
    public async Task JoinHubGroup_WithValidHubId_AddsClientToGroup()
    {
        // Arrange
        var hubId = "test-hub";

        // Act
        await _sut.JoinHubGroup(hubId);

        // Assert
        var expectedGroupName = $"hub:{hubId}";
        _groupManagerMock.Verify(g => g.AddToGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinHubGroup_WithEmptyHubId_DoesNotAddToGroup()
    {
        // Act
        await _sut.JoinHubGroup("");

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task JoinHubGroup_WithNullHubId_DoesNotAddToGroup()
    {
        // Act
        await _sut.JoinHubGroup(null!);

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region LeaveHubGroup Tests

    [Fact]
    public async Task LeaveHubGroup_WithValidHubId_RemovesClientFromGroup()
    {
        // Arrange
        var hubId = "test-hub";

        // Act
        await _sut.LeaveHubGroup(hubId);

        // Assert
        var expectedGroupName = $"hub:{hubId}";
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveHubGroup_WithEmptyHubId_DoesNotRemoveFromGroup()
    {
        // Act
        await _sut.LeaveHubGroup("");

        // Assert
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region JoinNodeGroup Tests

    [Fact]
    public async Task JoinNodeGroup_WithValidNodeId_AddsClientToGroup()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.JoinNodeGroup(nodeId);

        // Assert
        var expectedGroupName = $"node:{nodeId}";
        _groupManagerMock.Verify(g => g.AddToGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinNodeGroup_WithEmptyNodeId_DoesNotAddToGroup()
    {
        // Act
        await _sut.JoinNodeGroup(Guid.Empty);

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region LeaveNodeGroup Tests

    [Fact]
    public async Task LeaveNodeGroup_WithValidNodeId_RemovesClientFromGroup()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.LeaveNodeGroup(nodeId);

        // Assert
        var expectedGroupName = $"node:{nodeId}";
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveNodeGroup_WithEmptyNodeId_DoesNotRemoveFromGroup()
    {
        // Act
        await _sut.LeaveNodeGroup(Guid.Empty);

        // Assert
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region JoinAlertGroup Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task JoinAlertGroup_WithValidLevel_AddsClientToGroup(int level)
    {
        // Act
        await _sut.JoinAlertGroup(level);

        // Assert
        var expectedGroupName = $"alerts:{level}";
        _groupManagerMock.Verify(g => g.AddToGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public async Task JoinAlertGroup_WithInvalidLevel_DoesNotAddToGroup(int level)
    {
        // Act
        await _sut.JoinAlertGroup(level);

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region LeaveAlertGroup Tests

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task LeaveAlertGroup_WithValidLevel_RemovesClientFromGroup(int level)
    {
        // Act
        await _sut.LeaveAlertGroup(level);

        // Assert
        var expectedGroupName = $"alerts:{level}";
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(100)]
    public async Task LeaveAlertGroup_WithInvalidLevel_DoesNotRemoveFromGroup(int level)
    {
        // Act
        await _sut.LeaveAlertGroup(level);

        // Assert
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region JoinDebugGroup Tests

    [Fact]
    public async Task JoinDebugGroup_WithValidNodeId_AddsClientToGroup()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.JoinDebugGroup(nodeId);

        // Assert
        var expectedGroupName = $"debug:{nodeId}";
        _groupManagerMock.Verify(g => g.AddToGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinDebugGroup_WithEmptyNodeId_DoesNotAddToGroup()
    {
        // Act
        await _sut.JoinDebugGroup(Guid.Empty);

        // Assert
        _groupManagerMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region LeaveDebugGroup Tests

    [Fact]
    public async Task LeaveDebugGroup_WithValidNodeId_RemovesClientFromGroup()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        await _sut.LeaveDebugGroup(nodeId);

        // Assert
        var expectedGroupName = $"debug:{nodeId}";
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(_connectionId, expectedGroupName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveDebugGroup_WithEmptyNodeId_DoesNotRemoveFromGroup()
    {
        // Act
        await _sut.LeaveDebugGroup(Guid.Empty);

        // Assert
        _groupManagerMock.Verify(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Static Helper Methods Tests

    [Fact]
    public void GetTenantGroupName_ReturnsCorrectFormat()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var result = SensorHub.GetTenantGroupName(tenantId);

        // Assert
        result.Should().Be($"tenant:{tenantId}");
    }

    [Fact]
    public void GetHubGroupName_ReturnsCorrectFormat()
    {
        // Arrange
        var hubId = "my-hub";

        // Act
        var result = SensorHub.GetHubGroupName(hubId);

        // Assert
        result.Should().Be("hub:my-hub");
    }

    [Fact]
    public void GetNodeGroupName_ReturnsCorrectFormat()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var result = SensorHub.GetNodeGroupName(nodeId);

        // Assert
        result.Should().Be($"node:{nodeId}");
    }

    [Theory]
    [InlineData(0, "alerts:0")]
    [InlineData(1, "alerts:1")]
    [InlineData(2, "alerts:2")]
    [InlineData(3, "alerts:3")]
    public void GetAlertGroupName_ReturnsCorrectFormat(int level, string expected)
    {
        // Act
        var result = SensorHub.GetAlertGroupName(level);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetDebugGroupName_ReturnsCorrectFormat()
    {
        // Arrange
        var nodeId = Guid.NewGuid();

        // Act
        var result = SensorHub.GetDebugGroupName(nodeId);

        // Assert
        result.Should().Be($"debug:{nodeId}");
    }

    #endregion
}
