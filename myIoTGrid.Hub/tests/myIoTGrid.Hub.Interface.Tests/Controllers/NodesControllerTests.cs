using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;
using myIoTGrid.Hub.Interface.Hubs;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for NodesController.
/// New 3-tier model: Node has SensorAssignments (not Sensors directly).
/// Matter-konform: Node = ESP32/LoRa32 Device
/// </summary>
public class NodesControllerTests
{
    private readonly Mock<INodeService> _nodeServiceMock;
    private readonly Mock<IHubService> _hubServiceMock;
    private readonly Mock<INodeSensorAssignmentService> _assignmentServiceMock;
    private readonly Mock<ISensorService> _sensorServiceMock;
    private readonly Mock<IReadingService> _readingServiceMock;
    private readonly Mock<INodeDebugLogService> _debugLogServiceMock;
    private readonly Mock<IHubContext<SensorHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<NodesController>> _loggerMock;
    private readonly NodesController _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _hubId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private readonly Guid _sensorId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private readonly Guid _assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    public NodesControllerTests()
    {
        _nodeServiceMock = new Mock<INodeService>();
        _hubServiceMock = new Mock<IHubService>();
        _assignmentServiceMock = new Mock<INodeSensorAssignmentService>();
        _sensorServiceMock = new Mock<ISensorService>();
        _readingServiceMock = new Mock<IReadingService>();
        _debugLogServiceMock = new Mock<INodeDebugLogService>();
        _hubContextMock = new Mock<IHubContext<SensorHub>>();
        _clientProxyMock = new Mock<IClientProxy>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<NodesController>>();

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        hubClientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(c => c.Clients).Returns(hubClientsMock.Object);

        _sut = new NodesController(
            _nodeServiceMock.Object,
            _hubServiceMock.Object,
            _assignmentServiceMock.Object,
            _sensorServiceMock.Object,
            _readingServiceMock.Object,
            _debugLogServiceMock.Object,
            _hubContextMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);

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

    // NOTE: GetAssignments and GetAssignmentByEndpoint tests moved to NodeSensorAssignmentsControllerTests
    // These endpoints are now in NodeSensorAssignmentsController at:
    // GET /api/nodes/{nodeId:guid}/assignments
    // GET /api/nodes/{nodeId:guid}/assignments/endpoint/{endpointId:int}

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

        // GetOrCreate pattern: returns (NodeDto, isNew: true) for new nodes
        _nodeServiceMock.Setup(s => s.RegisterOrUpdateWithStatusAsync(It.IsAny<CreateNodeDto>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((node, true));

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(NodesController.GetById));
        createdResult.Value.Should().BeOfType<NodeDto>();
    }

    [Fact]
    public async Task Create_WithExistingNode_ReturnsOkWithExistingNode()
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
        var existingNode = CreateNodeDto("node-01", "Existing Node");

        // GetOrCreate pattern: returns (NodeDto, isNew: false) for existing nodes
        _nodeServiceMock.Setup(s => s.RegisterOrUpdateWithStatusAsync(It.IsAny<CreateNodeDto>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((existingNode, false));

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeDto>();
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
        // GetOrCreate pattern: returns (NodeDto, isNew: true) for new nodes
        _nodeServiceMock.Setup(s => s.RegisterOrUpdateWithStatusAsync(It.IsAny<CreateNodeDto>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((node, true));

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

    #region GetAll Without HubId Tests

    [Fact]
    public async Task GetAll_WithoutHubId_ReturnsAllNodes()
    {
        // Arrange
        var nodes = new List<NodeDto>
        {
            CreateNodeDto("node-01", "Test Node 1"),
            CreateNodeDto("node-02", "Test Node 2")
        };
        _nodeServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodes);

        // Act
        var result = await _sut.GetAll(null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNodes = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeDto>>().Subject;
        returnedNodes.Should().HaveCount(2);
    }

    #endregion

    #region GetPaged Tests

    [Fact]
    public async Task GetPaged_ReturnsPagedResult()
    {
        // Arrange
        var queryParams = new QueryParamsDto { Page = 1, Size = 10 };
        var pagedResult = new PagedResultDto<NodeDto>
        {
            Items = new List<NodeDto> { CreateNodeDto("node-01", "Test Node") },
            TotalRecords = 1,
            Page = 1,
            Size = 10
        };
        _nodeServiceMock.Setup(s => s.GetPagedAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetPaged(queryParams, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<PagedResultDto<NodeDto>>();
    }

    #endregion

    #region GetSensorsLatest Tests

    [Fact]
    public async Task GetSensorsLatest_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var nodeSensorsLatest = new NodeSensorsLatestDto(
            NodeId: _nodeId,
            NodeName: "Test Node",
            LocationName: "Wohnzimmer",
            Sensors: new List<SensorLatestReadingDto>()
        );
        _nodeServiceMock.Setup(s => s.GetSensorsLatestAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodeSensorsLatest);

        // Act
        var result = await _sut.GetSensorsLatest(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeSensorsLatestDto>();
    }

    [Fact]
    public async Task GetSensorsLatest_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.GetSensorsLatestAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeSensorsLatestDto?)null);

        // Act
        var result = await _sut.GetSensorsLatest(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Provision Tests

    [Fact]
    public async Task Provision_WithValidMacAddress_ReturnsOk()
    {
        // Arrange
        var dto = new NodeRegistrationDto(
            MacAddress: "AA:BB:CC:DD:EE:FF",
            FirmwareVersion: "1.0.0",
            Name: "Test Node"
        );
        var config = new NodeConfigurationDto(
            NodeId: "node-01",
            ApiKey: "mig_key_testkey",
            WifiSsid: "TestWiFi",
            WifiPassword: "password",
            HubApiUrl: "http://localhost:5000"
        );

        _configurationMock.Setup(c => c["NodeProvisioning:WifiSsid"]).Returns("TestWiFi");
        _configurationMock.Setup(c => c["NodeProvisioning:WifiPassword"]).Returns("password");
        _configurationMock.Setup(c => c["NodeProvisioning:HubApiUrl"]).Returns((string?)null);
        _nodeServiceMock.Setup(s => s.RegisterNodeAsync(
                dto, "TestWiFi", "password", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.Provision(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeConfigurationDto>();
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("NodeProvisioned", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Provision_WithEmptyMacAddress_ReturnsBadRequest()
    {
        // Arrange
        var dto = new NodeRegistrationDto(
            MacAddress: "",
            FirmwareVersion: "1.0.0",
            Name: null
        );

        // Act
        var result = await _sut.Provision(dto, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("MacAddress");
    }

    [Fact]
    public async Task Provision_WhenServiceThrows_ReturnsBadRequest()
    {
        // Arrange
        var dto = new NodeRegistrationDto(
            MacAddress: "AA:BB:CC:DD:EE:FF",
            FirmwareVersion: "1.0.0",
            Name: null
        );

        _configurationMock.Setup(c => c["NodeProvisioning:WifiSsid"]).Returns("TestWiFi");
        _configurationMock.Setup(c => c["NodeProvisioning:WifiPassword"]).Returns("password");
        _nodeServiceMock.Setup(s => s.RegisterNodeAsync(
                dto, "TestWiFi", "password", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Node already registered"));

        // Act
        var result = await _sut.Provision(dto, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequest.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("already registered");
    }

    #endregion

    #region Heartbeat Tests

    [Fact]
    public async Task Heartbeat_WithValidApiKey_ReturnsOk()
    {
        // Arrange
        var dto = new NodeHeartbeatDto(
            NodeId: "node-01",
            FirmwareVersion: "1.0.0",
            BatteryLevel: 85
        );
        var node = CreateNodeDto("node-01", "Test Node");
        var response = new NodeHeartbeatResponseDto(
            Success: true,
            ServerTime: DateTime.UtcNow,
            NextHeartbeatSeconds: 60
        );

        SetAuthorizationHeader("Bearer mig_key_validkey");
        _nodeServiceMock.Setup(s => s.ValidateApiKeyAsync("node-01", "mig_key_validkey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _nodeServiceMock.Setup(s => s.ProcessHeartbeatAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.Heartbeat(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeHeartbeatResponseDto>();
        _clientProxyMock.Verify(
            c => c.SendCoreAsync("NodeHeartbeat", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Heartbeat_WithoutAuthHeader_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new NodeHeartbeatDto(
            NodeId: "node-01",
            FirmwareVersion: "1.0.0",
            BatteryLevel: 85
        );

        SetAuthorizationHeader(null);

        // Act
        var result = await _sut.Heartbeat(dto, CancellationToken.None);

        // Assert
        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("API key required");
    }

    [Fact]
    public async Task Heartbeat_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new NodeHeartbeatDto(
            NodeId: "node-01",
            FirmwareVersion: "1.0.0",
            BatteryLevel: 85
        );

        SetAuthorizationHeader("Bearer mig_key_invalidkey");
        _nodeServiceMock.Setup(s => s.ValidateApiKeyAsync("node-01", "mig_key_invalidkey", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.Heartbeat(dto, CancellationToken.None);

        // Assert
        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problemDetails = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Invalid API key");
    }

    #endregion

    #region ValidateApiKey Tests

    [Fact]
    public async Task ValidateApiKey_WithValidKey_ReturnsOk()
    {
        // Arrange
        var node = CreateNodeDto("node-01", "Test Node");
        SetAuthorizationHeader("Bearer mig_key_validkey");
        _nodeServiceMock.Setup(s => s.ValidateApiKeyAsync("node-01", "mig_key_validkey", It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _sut.ValidateApiKey("node-01", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeDto>();
    }

    [Fact]
    public async Task ValidateApiKey_WithoutAuthHeader_ReturnsUnauthorized()
    {
        // Arrange
        SetAuthorizationHeader(null);

        // Act
        var result = await _sut.ValidateApiKey("node-01", CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ValidateApiKey_WithInvalidKey_ReturnsUnauthorized()
    {
        // Arrange
        SetAuthorizationHeader("Bearer mig_key_invalidkey");
        _nodeServiceMock.Setup(s => s.ValidateApiKeyAsync("node-01", "mig_key_invalidkey", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.ValidateApiKey("node-01", CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    #endregion

    #region RegenerateApiKey Tests

    [Fact]
    public async Task RegenerateApiKey_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var config = new NodeConfigurationDto(
            NodeId: "node-01",
            ApiKey: "mig_key_newkey",
            WifiSsid: "TestWiFi",
            WifiPassword: "password",
            HubApiUrl: "http://localhost:5000"
        );

        _configurationMock.Setup(c => c["NodeProvisioning:WifiSsid"]).Returns("TestWiFi");
        _configurationMock.Setup(c => c["NodeProvisioning:WifiPassword"]).Returns("password");
        _configurationMock.Setup(c => c["NodeProvisioning:HubApiUrl"]).Returns((string?)null);
        _nodeServiceMock.Setup(s => s.RegenerateApiKeyAsync(
                _nodeId, "TestWiFi", "password", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.RegenerateApiKey(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeConfigurationDto>();
    }

    [Fact]
    public async Task RegenerateApiKey_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _configurationMock.Setup(c => c["NodeProvisioning:WifiSsid"]).Returns("TestWiFi");
        _configurationMock.Setup(c => c["NodeProvisioning:WifiPassword"]).Returns("password");
        _nodeServiceMock.Setup(s => s.RegenerateApiKeyAsync(
                _nodeId, "TestWiFi", "password", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeConfigurationDto?)null);

        // Act
        var result = await _sut.RegenerateApiKey(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetByMacAddress Tests

    [Fact]
    public async Task GetByMacAddress_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var node = CreateNodeDto("node-01", "Test Node");
        _nodeServiceMock.Setup(s => s.GetByMacAddressAsync("AA:BB:CC:DD:EE:FF", It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);

        // Act
        var result = await _sut.GetByMacAddress("AA:BB:CC:DD:EE:FF", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeDto>();
    }

    [Fact]
    public async Task GetByMacAddress_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.GetByMacAddressAsync("AA:BB:CC:DD:EE:FF", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.GetByMacAddress("AA:BB:CC:DD:EE:FF", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetConfiguration Tests

    [Fact]
    public async Task GetConfiguration_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var nodes = new List<NodeDto> { CreateNodeDto("node-01", "Test Node") };
        var assignments = new List<NodeSensorAssignmentDto>
        {
            CreateAssignmentDto(1, "temperature")
        };

        _nodeServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(nodes);
        _assignmentServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        // Act
        var result = await _sut.GetConfiguration("node-01", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeSensorConfigurationDto>();
    }

    [Fact]
    public async Task GetConfiguration_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeDto>());

        // Act
        var result = await _sut.GetConfiguration("unknown-node", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetGpsStatus Tests

    [Fact]
    public async Task GetGpsStatus_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var node = CreateNodeDto("node-01", "Test Node");
        var readings = new List<ReadingDto>
        {
            CreateReadingDto("gps_satellites", 8),
            CreateReadingDto("gps_fix", 3),
            CreateReadingDto("gps_hdop", 1.5),
            CreateReadingDto("latitude", 50.9375),
            CreateReadingDto("longitude", 6.9603)
        };

        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _readingServiceMock.Setup(s => s.GetLatestByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetGpsStatus(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gpsStatus = okResult.Value.Should().BeOfType<NodeGpsStatusDto>().Subject;
        gpsStatus.HasGps.Should().BeTrue();
        gpsStatus.Satellites.Should().Be(8);
        gpsStatus.FixType.Should().Be(3);
        gpsStatus.HdopQuality.Should().Be("Excellent");
    }

    [Fact]
    public async Task GetGpsStatus_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDto?)null);

        // Act
        var result = await _sut.GetGpsStatus(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetGpsStatus_WithNoGpsReadings_ReturnsHasGpsFalse()
    {
        // Arrange
        var node = CreateNodeDto("node-01", "Test Node");
        var readings = new List<ReadingDto>
        {
            CreateReadingDto("temperature", 21.5)
        };

        _nodeServiceMock.Setup(s => s.GetByIdAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(node);
        _readingServiceMock.Setup(s => s.GetLatestByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetGpsStatus(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gpsStatus = okResult.Value.Should().BeOfType<NodeGpsStatusDto>().Subject;
        gpsStatus.HasGps.Should().BeFalse();
    }

    #endregion

    #region Register Tests

    [Fact]
    public async Task Register_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var dto = new RegisterNodeDto(
            SerialNumber: "ESP32-0070078492CC",
            Name: "Wohnzimmer Sensor",
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32",
            Capabilities: new List<string> { "temperature", "humidity" },
            Location: null
        );
        var defaultHub = new HubDto(
            Id: _hubId,
            TenantId: _tenantId,
            HubId: "hub-01",
            Name: "Default Hub",
            Description: null,
            LastSeen: DateTime.UtcNow,
            IsOnline: true,
            CreatedAt: DateTime.UtcNow,
            SensorCount: 5
        );
        var node = CreateNodeDto("ESP32-0070078492CC", "Wohnzimmer Sensor");

        _hubServiceMock.Setup(s => s.GetDefaultHubAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultHub);
        _nodeServiceMock.Setup(s => s.RegisterOrUpdateWithStatusAsync(
                It.IsAny<CreateNodeDto>(), "1.0.0", It.IsAny<CancellationToken>()))
            .ReturnsAsync((node, true));

        // Act
        var result = await _sut.Register(dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<NodeRegistrationResponseDto>().Subject;
        response.SerialNumber.Should().Be("ESP32-0070078492CC");
        response.IsNewNode.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithEmptySerialNumber_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterNodeDto(
            SerialNumber: "",
            Name: "Test",
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32",
            Capabilities: null,
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

    #region Debug Configuration Tests

    [Fact]
    public async Task GetDebugConfiguration_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var config = new NodeDebugConfigurationDto(
            NodeId: _nodeId,
            SerialNumber: "ESP32-0070078492CC",
            DebugLevel: DebugLevelDto.Debug,
            EnableRemoteLogging: true,
            LastDebugChange: DateTime.UtcNow
        );

        _debugLogServiceMock.Setup(s => s.GetDebugConfigurationAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.GetDebugConfiguration(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeDebugConfigurationDto>();
    }

    [Fact]
    public async Task GetDebugConfiguration_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        _debugLogServiceMock.Setup(s => s.GetDebugConfigurationAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDebugConfigurationDto?)null);

        // Act
        var result = await _sut.GetDebugConfiguration(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetDebugLevel_WithExistingNode_ReturnsOk()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Debug, true);
        var config = new NodeDebugConfigurationDto(
            NodeId: _nodeId,
            SerialNumber: "ESP32-0070078492CC",
            DebugLevel: DebugLevelDto.Debug,
            EnableRemoteLogging: true,
            LastDebugChange: DateTime.UtcNow
        );

        _debugLogServiceMock.Setup(s => s.SetDebugLevelAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.SetDebugLevel(_nodeId, dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeDebugConfigurationDto>();
    }

    [Fact]
    public async Task SetDebugLevel_WithNonExistingNode_ReturnsNotFound()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Debug, true);

        _debugLogServiceMock.Setup(s => s.SetDebugLevelAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDebugConfigurationDto?)null);

        // Act
        var result = await _sut.SetDebugLevel(_nodeId, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private void SetAuthorizationHeader(string? value)
    {
        if (value != null)
        {
            _sut.ControllerContext.HttpContext.Request.Headers.Authorization = value;
        }
        else
        {
            _sut.ControllerContext.HttpContext.Request.Headers.Remove("Authorization");
        }
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
            BaudRateOverride: null,
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
                BaudRate: null,
                IntervalSeconds: 60,
                OffsetCorrection: 0.0,
                GainCorrection: 1.0
            )
        );
    }

    private ReadingDto CreateReadingDto(string measurementType, double value)
    {
        return new ReadingDto(
            Id: 1,
            TenantId: _tenantId,
            NodeId: _nodeId,
            NodeName: "Test Node",
            AssignmentId: _assignmentId,
            SensorId: _sensorId,
            SensorCode: "test-sensor",
            SensorName: "Test Sensor",
            SensorIcon: "thermostat",
            SensorColor: "#FF5722",
            MeasurementType: measurementType,
            DisplayName: measurementType,
            RawValue: value,
            Value: value,
            Unit: measurementType == "temperature" ? "°C" : "",
            Timestamp: DateTime.UtcNow,
            Location: null,
            IsSyncedToCloud: false
        );
    }

    #endregion
}
