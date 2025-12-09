using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for HubController.
/// Single-Hub-Architecture: Only one Hub per Tenant/Installation allowed.
/// Hub = Raspberry Pi Gateway
/// </summary>
public class HubControllerTests
{
    private readonly Mock<IHubService> _hubServiceMock;
    private readonly Mock<INodeService> _nodeServiceMock;
    private readonly HubController _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public HubControllerTests()
    {
        _hubServiceMock = new Mock<IHubService>();
        _nodeServiceMock = new Mock<INodeService>();
        _sut = new HubController(_hubServiceMock.Object, _nodeServiceMock.Object);
    }

    #region Get (GetCurrentHub) Tests

    [Fact]
    public async Task Get_ReturnsOkWithHub()
    {
        // Arrange
        var hub = CreateHubDto("my-iot-hub", "My IoT Hub");

        _hubServiceMock.Setup(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hub);

        // Act
        var result = await _sut.Get(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedHub = okResult.Value.Should().BeOfType<HubDto>().Subject;
        returnedHub.HubId.Should().Be("my-iot-hub");
    }

    [Fact]
    public async Task Get_CallsGetCurrentHubAsync()
    {
        // Arrange
        _hubServiceMock.Setup(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateHubDto("my-iot-hub", "My IoT Hub"));

        // Act
        await _sut.Get(CancellationToken.None);

        // Assert
        _hubServiceMock.Verify(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOkWithHub()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: "Updated Hub", Description: "Updated description");
        var hub = CreateHubDto("my-iot-hub", "Updated Hub");

        _hubServiceMock.Setup(s => s.UpdateCurrentHubAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hub);

        // Act
        var result = await _sut.Update(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedHub = okResult.Value.Should().BeOfType<HubDto>().Subject;
        returnedHub.Name.Should().Be("Updated Hub");
    }

    [Fact]
    public async Task Update_WithPartialData_ReturnsOk()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: "Only Name Updated", Description: null);
        var hub = CreateHubDto("my-iot-hub", "Only Name Updated");

        _hubServiceMock.Setup(s => s.UpdateCurrentHubAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(hub);

        // Act
        var result = await _sut.Update(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Update_CallsUpdateCurrentHubAsync()
    {
        // Arrange
        var dto = new UpdateHubDto(Name: "Test", Description: null);
        _hubServiceMock.Setup(s => s.UpdateCurrentHubAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateHubDto("my-iot-hub", "Test"));

        // Act
        await _sut.Update(dto, CancellationToken.None);

        // Assert
        _hubServiceMock.Verify(s => s.UpdateCurrentHubAsync(dto, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_ReturnsOkWithStatus()
    {
        // Arrange
        var services = new ServiceStatusDto(
            Api: new ServiceState(true),
            Database: new ServiceState(true),
            Mqtt: new ServiceState(true),
            Cloud: new ServiceState(false, "Not configured")
        );
        var status = new HubStatusDto(
            IsOnline: true,
            LastSeen: DateTime.UtcNow,
            NodeCount: 5,
            OnlineNodeCount: 3,
            Services: services
        );

        _hubServiceMock.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.GetStatus(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStatus = okResult.Value.Should().BeOfType<HubStatusDto>().Subject;
        returnedStatus.IsOnline.Should().BeTrue();
        returnedStatus.NodeCount.Should().Be(5);
        returnedStatus.OnlineNodeCount.Should().Be(3);
    }

    [Fact]
    public async Task GetStatus_CallsGetStatusAsync()
    {
        // Arrange
        var services = new ServiceStatusDto(
            Api: new ServiceState(true),
            Database: new ServiceState(true),
            Mqtt: new ServiceState(false),
            Cloud: new ServiceState(false)
        );
        _hubServiceMock.Setup(s => s.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubStatusDto(true, DateTime.UtcNow, 0, 0, services));

        // Act
        await _sut.GetStatus(CancellationToken.None);

        // Assert
        _hubServiceMock.Verify(s => s.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetNodes Tests

    [Fact]
    public async Task GetNodes_ReturnsOkWithNodes()
    {
        // Arrange
        var hub = CreateHubDto("my-iot-hub", "My IoT Hub");
        var nodes = new List<NodeDto>
        {
            CreateNodeDto("node-01", "Node 1"),
            CreateNodeDto("node-02", "Node 2")
        };

        _hubServiceMock.Setup(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hub);
        _nodeServiceMock.Setup(s => s.GetByHubAsync(_hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodes);

        // Act
        var result = await _sut.GetNodes(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNodes = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeDto>>().Subject;
        returnedNodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetNodes_WithNoNodes_ReturnsOkWithEmptyList()
    {
        // Arrange
        var hub = CreateHubDto("my-iot-hub", "My IoT Hub");

        _hubServiceMock.Setup(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hub);
        _nodeServiceMock.Setup(s => s.GetByHubAsync(_hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeDto>());

        // Act
        var result = await _sut.GetNodes(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNodes = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeDto>>().Subject;
        returnedNodes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetNodes_GetsCurrentHubFirst()
    {
        // Arrange
        var hub = CreateHubDto("my-iot-hub", "My IoT Hub");

        _hubServiceMock.Setup(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(hub);
        _nodeServiceMock.Setup(s => s.GetByHubAsync(_hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeDto>());

        // Act
        await _sut.GetNodes(CancellationToken.None);

        // Assert
        _hubServiceMock.Verify(s => s.GetCurrentHubAsync(It.IsAny<CancellationToken>()), Times.Once);
        _nodeServiceMock.Verify(s => s.GetByHubAsync(_hubId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetProvisioningSettings Tests

    [Fact]
    public async Task GetProvisioningSettings_ReturnsOkWithSettings()
    {
        // Arrange
        var settings = new HubProvisioningSettingsDto(
            DefaultWifiSsid: "MyNetwork",
            DefaultWifiPassword: "password123",
            ApiUrl: "http://192.168.1.100",
            ApiPort: 5000
        );

        _hubServiceMock.Setup(s => s.GetProvisioningSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await _sut.GetProvisioningSettings(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedSettings = okResult.Value.Should().BeOfType<HubProvisioningSettingsDto>().Subject;
        returnedSettings.DefaultWifiSsid.Should().Be("MyNetwork");
        returnedSettings.ApiUrl.Should().Be("http://192.168.1.100");
    }

    [Fact]
    public async Task GetProvisioningSettings_CallsGetProvisioningSettingsAsync()
    {
        // Arrange
        _hubServiceMock.Setup(s => s.GetProvisioningSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubProvisioningSettingsDto("wifi", "pass", "http://localhost", 5000));

        // Act
        await _sut.GetProvisioningSettings(CancellationToken.None);

        // Assert
        _hubServiceMock.Verify(s => s.GetProvisioningSettingsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private HubDto CreateHubDto(string hubId, string name)
    {
        return new HubDto(
            Id: _hubId,
            TenantId: _tenantId,
            HubId: hubId,
            Name: name,
            Description: null,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 0
        );
    }

    private NodeDto CreateNodeDto(string nodeId, string name)
    {
        return new NodeDto(
            Id: Guid.NewGuid(),
            HubId: _hubId,
            NodeId: nodeId,
            Name: name,
            Protocol: ProtocolDto.WLAN,
            Location: null,
            AssignmentCount: 2,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            FirmwareVersion: "1.0.0",
            BatteryLevel: 100,
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
