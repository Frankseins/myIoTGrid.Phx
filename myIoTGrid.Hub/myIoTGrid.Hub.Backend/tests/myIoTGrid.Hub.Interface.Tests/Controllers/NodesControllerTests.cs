using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;
using myIoTGrid.Hub.Interface.Hubs;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.Enums;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for NodesController.
/// New 3-tier model: Node has SensorAssignments (not Sensors directly).
/// Matter-konform: Node = ESP32/LoRa32 Device
/// </summary>
public class NodesControllerTests
{
    private readonly Mock<INodeService> _nodeServiceMock;
    private readonly Mock<INodeSensorAssignmentService> _assignmentServiceMock;
    private readonly Mock<IHubService> _hubServiceMock;
    private readonly Mock<IHubContext<SensorHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly NodesController _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private readonly Guid _sensorId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private readonly Guid _assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    public NodesControllerTests()
    {
        _nodeServiceMock = new Mock<INodeService>();
        _assignmentServiceMock = new Mock<INodeSensorAssignmentService>();
        _hubServiceMock = new Mock<IHubService>();
        _hubContextMock = new Mock<IHubContext<SensorHub>>();
        _clientProxyMock = new Mock<IClientProxy>();
        _configurationMock = new Mock<IConfiguration>();

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(hubClientsMock.Object);

        _sut = new NodesController(
            _nodeServiceMock.Object,
            _assignmentServiceMock.Object,
            _hubServiceMock.Object,
            _hubContextMock.Object,
            _configurationMock.Object);

        // Setup HttpContext for Register endpoint tests
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 5000);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithNodes()
    {
        // Arrange
        var nodes = new List<NodeDto>
        {
            CreateNodeDto("node-01", "Test Node 1"),
            CreateNodeDto("node-02", "Test Node 2")
        };
        _nodeServiceMock.Setup(s => s.GetByHubAsync(_hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodes);

        // Act
        var result = await _sut.GetAll(_hubId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNodes = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeDto>>().Subject;
        returnedNodes.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_WithEmptyResult_ReturnsOkWithEmptyList()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.GetByHubAsync(_hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeDto>());

        // Act
        var result = await _sut.GetAll(_hubId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNodes = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeDto>>().Subject;
        returnedNodes.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingNode_ReturnsOkWithNode()
    {
        // Arrange
        var node = CreateNodeDto("node-01", "Test Node");
        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _sut.GetById(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNode = okResult.Value.Should().BeOfType<NodeDto>().Subject;
        returnedNode.NodeId.Should().Be("node-01");
    }

    [Fact]
    public async Task GetById_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.GetById(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetAssignments Tests

    [Fact]
    public async Task GetAssignments_ReturnsOkWithAssignments()
    {
        // Arrange
        var assignments = new List<NodeSensorAssignmentDto>
        {
            CreateAssignmentDto(1, "temperature"),
            CreateAssignmentDto(2, "humidity")
        };
        _assignmentServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        // Act
        var result = await _sut.GetAssignments(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignments = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeSensorAssignmentDto>>().Subject;
        returnedAssignments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAssignments_WithNoAssignments_ReturnsEmptyList()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeSensorAssignmentDto>());

        // Act
        var result = await _sut.GetAssignments(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignments = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeSensorAssignmentDto>>().Subject;
        returnedAssignments.Should().BeEmpty();
    }

    #endregion

    #region GetAssignmentByEndpoint Tests

    [Fact]
    public async Task GetAssignmentByEndpoint_WithExistingAssignment_ReturnsOk()
    {
        // Arrange
        var assignment = CreateAssignmentDto(1, "temperature");
        _assignmentServiceMock.Setup(s => s.GetByEndpointAsync(_nodeId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Act
        var result = await _sut.GetAssignmentByEndpoint(_nodeId, 1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignment = okResult.Value.Should().BeOfType<NodeSensorAssignmentDto>().Subject;
        returnedAssignment.EndpointId.Should().Be(1);
    }

    [Fact]
    public async Task GetAssignmentByEndpoint_WithNonExistingAssignment_ReturnsNotFound()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetByEndpointAsync(_nodeId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeSensorAssignmentDto?)null);

        // Act
        var result = await _sut.GetAssignmentByEndpoint(_nodeId, 99, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ReturnsOkWithRegistrationResponse()
    {
        // Arrange
        var dto = new RegisterNodeDto(
            SerialNumber: "node-01",
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32",
            Capabilities: new List<string> { "temperature", "humidity" },
            Name: "Test Node",
            Location: null
        );
        var node = CreateNodeDto("node-01", "Test Node");
        var defaultHub = new HubDto(
            Id: _hubId,
            TenantId: _tenantId,
            HubId: "default-hub",
            Name: "Default Hub",
            Description: null,
            LastSeen: null,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 0
        );

        _hubServiceMock.Setup(s => s.GetDefaultHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultHub);
        _nodeServiceMock.Setup(s => s.RegisterOrUpdateWithStatusAsync(It.IsAny<CreateNodeDto>(), "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((node, true));

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeRegistrationResponseDto>();

        _clientProxyMock.Verify(
            c => c.SendCoreAsync("NodeRegistered", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Register_WithLoRaHardwareType_SetsLoRaWANProtocol()
    {
        // Arrange
        var dto = new RegisterNodeDto(
            SerialNumber: "lora-node-01",
            FirmwareVersion: "1.0.0",
            HardwareType: "LORA",
            Capabilities: new List<string> { "temperature" },
            Name: "LoRa Node",
            Location: null
        );
        var node = CreateNodeDto("lora-node-01", "LoRa Node");
        var defaultHub = new HubDto(
            Id: _hubId,
            TenantId: _tenantId,
            HubId: "default-hub",
            Name: "Default Hub",
            Description: null,
            LastSeen: null,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 0
        );

        _hubServiceMock.Setup(s => s.GetDefaultHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultHub);
        _nodeServiceMock.Setup(s => s.RegisterOrUpdateWithStatusAsync(
                It.Is<CreateNodeDto>(d => d.Protocol == ProtocolDto.LoRaWAN),
                "1.0.0",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((node, true));

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _nodeServiceMock.Verify(s => s.RegisterOrUpdateWithStatusAsync(
            It.Is<CreateNodeDto>(d => d.Protocol == ProtocolDto.LoRaWAN),
            "1.0.0",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithoutSerialNumber_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterNodeDto(
            SerialNumber: "",
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32",
            Capabilities: null,
            Name: null,
            Location: null
        );

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("SerialNumber");
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "node-01",
            Name: "Test Node",
            HubIdentifier: null,
            HubId: _hubId,
            Protocol: ProtocolDto.WLAN,
            Location: null
        );
        var node = CreateNodeDto("node-01", "Test Node");

        _nodeServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateNodeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(NodesController.GetById));
        createdResult.Value.Should().BeOfType<NodeDto>();
    }

    [Fact]
    public async Task Create_WithHubIdentifier_LooksUpHub()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "node-01",
            Name: "Test Node",
            HubIdentifier: "hub-01",
            HubId: null,
            Protocol: ProtocolDto.WLAN,
            Location: null
        );
        var hubDto = new HubDto(
            Id: _hubId,
            TenantId: _tenantId,
            HubId: "hub-01",
            Name: "Test Hub",
            Description: null,
            LastSeen: null,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 0
        );
        var node = CreateNodeDto("node-01", "Test Node");

        _hubServiceMock.Setup(s => s.GetOrCreateByHubIdAsync("hub-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(hubDto);
        _nodeServiceMock.Setup(s => s.CreateAsync(It.IsAny<CreateNodeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_WithoutHubIdOrIdentifier_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNodeDto(
            NodeId: "node-01",
            Name: "Test Node",
            HubIdentifier: null,
            HubId: null,
            Protocol: ProtocolDto.WLAN,
            Location: null
        );

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithExistingNode_ReturnsOkWithNode()
    {
        // Arrange
        var dto = new UpdateNodeDto(Name: "Updated Node", Location: null);
        var node = CreateNodeDto("node-01", "Updated Node");

        _nodeServiceMock.Setup(s => s.UpdateAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _sut.Update(_nodeId, dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNode = okResult.Value.Should().BeOfType<NodeDto>().Subject;
        returnedNode.Name.Should().Be("Updated Node");
    }

    [Fact]
    public async Task Update_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateNodeDto(Name: "Updated Node", Location: null);

        _nodeServiceMock.Setup(s => s.UpdateAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.Update(_nodeId, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region UpdateStatus Tests

    [Fact]
    public async Task UpdateStatus_WithExistingNode_ReturnsNoContent()
    {
        // Arrange
        var statusDto = new NodeStatusDto(NodeId: _nodeId, IsOnline: true, LastSeen: DateTime.UtcNow, BatteryLevel: 85);
        var node = CreateNodeDto("node-01", "Test Node");

        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _nodeServiceMock.Setup(s => s.UpdateStatusAsync(_nodeId, statusDto, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.UpdateStatus(_nodeId, statusDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("NodeStatusChanged", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatus_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        var statusDto = new NodeStatusDto(NodeId: _nodeId, IsOnline: true, LastSeen: DateTime.UtcNow, BatteryLevel: 85);

        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.UpdateStatus(_nodeId, statusDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingNode_ReturnsNoContent()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.DeleteAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.Delete(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.DeleteAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.Delete(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Helper Methods

    private NodeDto CreateNodeDto(string nodeId, string name)
    {
        return new NodeDto(
            Id: _nodeId,
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
            Status: NodeProvisioningStatusDto.Configured
        );
    }

    private NodeSensorAssignmentDto CreateAssignmentDto(int endpointId, string measurementType)
    {
        // v3.0 Two-Tier: NodeSensorAssignmentDto has SensorId, SensorCode, SensorName (no SensorTypeId, SensorTypeCode)
        return new NodeSensorAssignmentDto(
            Id: _assignmentId,
            NodeId: _nodeId,
            NodeName: "Test Node",
            SensorId: _sensorId,
            SensorCode: measurementType == "temperature" ? "dht22-01" : "dht22-02",
            SensorName: measurementType == "temperature" ? "DHT22 Temperature Sensor" : "DHT22 Humidity Sensor",
            EndpointId: endpointId,
            Alias: null,
            I2CAddressOverride: null,
            SdaPinOverride: null,
            SclPinOverride: null,
            OneWirePinOverride: null,
            AnalogPinOverride: null,
            DigitalPinOverride: null,
            TriggerPinOverride: null,
            EchoPinOverride: null,
            IntervalSecondsOverride: null,
            IsActive: true,
            LastSeenAt: DateTime.UtcNow,
            AssignedAt: DateTime.UtcNow,
            EffectiveConfig: new EffectiveConfigDto(
                I2CAddress: "0x76",
                SdaPin: 21,
                SclPin: 22,
                OneWirePin: null,
                AnalogPin: null,
                DigitalPin: null,
                TriggerPin: null,
                EchoPin: null,
                IntervalSeconds: 60,
                OffsetCorrection: 0.0,
                GainCorrection: 1.0
            )
        );
    }

    #endregion
}
