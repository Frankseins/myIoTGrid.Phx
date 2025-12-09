using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;
using myIoTGrid.Hub.Interface.Hubs;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for NodeSensorAssignmentsController.
/// </summary>
public class NodeSensorAssignmentsControllerTests
{
    private readonly Mock<INodeSensorAssignmentService> _assignmentServiceMock;
    private readonly Mock<IHubContext<SensorHub>> _hubContextMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly NodeSensorAssignmentsController _sut;

    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _sensorId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public NodeSensorAssignmentsControllerTests()
    {
        _assignmentServiceMock = new Mock<INodeSensorAssignmentService>();
        _hubContextMock = new Mock<IHubContext<SensorHub>>();
        _clientProxyMock = new Mock<IClientProxy>();

        var hubClientsMock = new Mock<IHubClients>();
        hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);
        _hubContextMock.Setup(h => h.Clients).Returns(hubClientsMock.Object);

        _sut = new NodeSensorAssignmentsController(
            _assignmentServiceMock.Object,
            _hubContextMock.Object);
    }

    #region GetByNode Tests

    [Fact]
    public async Task GetByNode_ReturnsOkWithAssignments()
    {
        // Arrange
        var assignments = new List<NodeSensorAssignmentDto>
        {
            CreateAssignmentDto(_assignmentId, 1),
            CreateAssignmentDto(Guid.NewGuid(), 2)
        };

        _assignmentServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        // Act
        var result = await _sut.GetByNode(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignments = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeSensorAssignmentDto>>().Subject;
        returnedAssignments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByNode_WithNoAssignments_ReturnsEmptyList()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeSensorAssignmentDto>());

        // Act
        var result = await _sut.GetByNode(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignments = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeSensorAssignmentDto>>().Subject;
        returnedAssignments.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingAssignment_ReturnsOk()
    {
        // Arrange
        var assignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Act
        var result = await _sut.GetById(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignment = okResult.Value.Should().BeOfType<NodeSensorAssignmentDto>().Subject;
        returnedAssignment.Id.Should().Be(_assignmentId);
    }

    [Fact]
    public async Task GetById_WithNonExistingAssignment_ReturnsNotFound()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeSensorAssignmentDto?)null);

        // Act
        var result = await _sut.GetById(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_WithWrongNodeId_ReturnsNotFound()
    {
        // Arrange
        var differentNodeId = Guid.NewGuid();
        var assignment = CreateAssignmentDto(_assignmentId, 1, differentNodeId);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Act
        var result = await _sut.GetById(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetByEndpoint Tests

    [Fact]
    public async Task GetByEndpoint_WithExistingAssignment_ReturnsOk()
    {
        // Arrange
        var assignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByEndpointAsync(_nodeId, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Act
        var result = await _sut.GetByEndpoint(_nodeId, 1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignment = okResult.Value.Should().BeOfType<NodeSensorAssignmentDto>().Subject;
        returnedAssignment.EndpointId.Should().Be(1);
    }

    [Fact]
    public async Task GetByEndpoint_WithNonExistingEndpoint_ReturnsNotFound()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetByEndpointAsync(_nodeId, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeSensorAssignmentDto?)null);

        // Act
        var result = await _sut.GetByEndpoint(_nodeId, 99, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1,
            Alias: "Living Room Temperature"
        );
        var assignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.CreateAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Act
        var result = await _sut.Create(_nodeId, dto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(NodeSensorAssignmentsController.GetById));
        createdResult.Value.Should().BeOfType<NodeSensorAssignmentDto>();
    }

    [Fact]
    public async Task Create_WithValidData_NotifiesSignalR()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1
        );
        var assignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.CreateAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignment);

        // Act
        await _sut.Create(_nodeId, dto, CancellationToken.None);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "AssignmentCreated",
            It.Is<object?[]>(o => o.Length == 1 && Equals(o[0], assignment)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithDuplicateEndpoint_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: _sensorId,
            EndpointId: 1
        );

        _assignmentServiceMock.Setup(s => s.CreateAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("EndpointId 1 is already assigned"));

        // Act
        var result = await _sut.Create(_nodeId, dto, CancellationToken.None);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("EndpointId");
    }

    [Fact]
    public async Task Create_WithInvalidSensor_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNodeSensorAssignmentDto(
            SensorId: Guid.NewGuid(),
            EndpointId: 1
        );

        _assignmentServiceMock.Setup(s => s.CreateAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Sensor not found"));

        // Act
        var result = await _sut.Create(_nodeId, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_WithValidData_ReturnsOk()
    {
        // Arrange
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1);
        var dto = new UpdateNodeSensorAssignmentDto(
            Alias: "Updated Alias",
            IsActive: true
        );
        var updatedAssignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);
        _assignmentServiceMock.Setup(s => s.UpdateAsync(_assignmentId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAssignment);

        // Act
        var result = await _sut.Update(_nodeId, _assignmentId, dto, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<NodeSensorAssignmentDto>();
    }

    [Fact]
    public async Task Update_WithValidData_NotifiesSignalR()
    {
        // Arrange
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1);
        var dto = new UpdateNodeSensorAssignmentDto(
            Alias: "Updated Alias"
        );
        var updatedAssignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);
        _assignmentServiceMock.Setup(s => s.UpdateAsync(_assignmentId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAssignment);

        // Act
        await _sut.Update(_nodeId, _assignmentId, dto, CancellationToken.None);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "AssignmentUpdated",
            It.Is<object?[]>(o => o.Length == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WithNonExistingAssignment_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateNodeSensorAssignmentDto(Alias: "Test");

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeSensorAssignmentDto?)null);

        // Act
        var result = await _sut.Update(_nodeId, _assignmentId, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WithWrongNodeId_ReturnsNotFound()
    {
        // Arrange
        var differentNodeId = Guid.NewGuid();
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1, differentNodeId);
        var dto = new UpdateNodeSensorAssignmentDto(Alias: "Test");

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);

        // Act
        var result = await _sut.Update(_nodeId, _assignmentId, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WhenServiceThrows_ReturnsNotFound()
    {
        // Arrange
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1);
        var dto = new UpdateNodeSensorAssignmentDto(Alias: "Test");

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);
        _assignmentServiceMock.Setup(s => s.UpdateAsync(_assignmentId, dto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Assignment not found"));

        // Act
        var result = await _sut.Update(_nodeId, _assignmentId, dto, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("not found");
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingAssignment_ReturnsNoContent()
    {
        // Arrange
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);
        _assignmentServiceMock.Setup(s => s.DeleteAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Delete(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WithExistingAssignment_NotifiesSignalR()
    {
        // Arrange
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);
        _assignmentServiceMock.Setup(s => s.DeleteAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Delete(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        _clientProxyMock.Verify(c => c.SendCoreAsync(
            "AssignmentDeleted",
            It.Is<object?[]>(o => o.Length == 1 && (Guid)o[0]! == _assignmentId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistingAssignment_ReturnsNotFound()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeSensorAssignmentDto?)null);

        // Act
        var result = await _sut.Delete(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WithWrongNodeId_ReturnsNotFound()
    {
        // Arrange
        var differentNodeId = Guid.NewGuid();
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1, differentNodeId);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);

        // Act
        var result = await _sut.Delete(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WhenServiceThrows_ReturnsNotFound()
    {
        // Arrange
        var existingAssignment = CreateAssignmentDto(_assignmentId, 1);

        _assignmentServiceMock.Setup(s => s.GetByIdAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAssignment);
        _assignmentServiceMock.Setup(s => s.DeleteAsync(_assignmentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete"));

        // Act
        var result = await _sut.Delete(_nodeId, _assignmentId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problemDetails = notFoundResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Cannot delete");
    }

    #endregion

    #region Helper Methods

    private NodeSensorAssignmentDto CreateAssignmentDto(Guid id, int endpointId, Guid? nodeId = null)
    {
        return new NodeSensorAssignmentDto(
            Id: id,
            NodeId: nodeId ?? _nodeId,
            NodeName: "Test Node",
            SensorId: _sensorId,
            SensorCode: "BME280",
            SensorName: "BME280 Sensor",
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
            LastSeenAt: null,
            AssignedAt: DateTime.UtcNow,
            EffectiveConfig: new EffectiveConfigDto(
                IntervalSeconds: 60,
                I2CAddress: "0x76",
                SdaPin: 21,
                SclPin: 22,
                OneWirePin: null,
                AnalogPin: null,
                DigitalPin: null,
                TriggerPin: null,
                EchoPin: null,
                BaudRate: null,
                OffsetCorrection: 0,
                GainCorrection: 1
            )
        );
    }

    #endregion
}

/// <summary>
/// Tests for SensorAssignmentsController.
/// </summary>
public class SensorAssignmentsControllerTests
{
    private readonly Mock<INodeSensorAssignmentService> _assignmentServiceMock;
    private readonly SensorAssignmentsController _sut;

    private readonly Guid _sensorId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public SensorAssignmentsControllerTests()
    {
        _assignmentServiceMock = new Mock<INodeSensorAssignmentService>();
        _sut = new SensorAssignmentsController(_assignmentServiceMock.Object);
    }

    [Fact]
    public async Task GetBySensor_ReturnsOkWithAssignments()
    {
        // Arrange
        var assignments = new List<NodeSensorAssignmentDto>
        {
            CreateAssignmentDto(_assignmentId, 1),
            CreateAssignmentDto(Guid.NewGuid(), 2)
        };

        _assignmentServiceMock.Setup(s => s.GetBySensorAsync(_sensorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignments);

        // Act
        var result = await _sut.GetBySensor(_sensorId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignments = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeSensorAssignmentDto>>().Subject;
        returnedAssignments.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetBySensor_WithNoAssignments_ReturnsEmptyList()
    {
        // Arrange
        _assignmentServiceMock.Setup(s => s.GetBySensorAsync(_sensorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeSensorAssignmentDto>());

        // Act
        var result = await _sut.GetBySensor(_sensorId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAssignments = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeSensorAssignmentDto>>().Subject;
        returnedAssignments.Should().BeEmpty();
    }

    private NodeSensorAssignmentDto CreateAssignmentDto(Guid id, int endpointId)
    {
        return new NodeSensorAssignmentDto(
            Id: id,
            NodeId: _nodeId,
            NodeName: "Test Node",
            SensorId: _sensorId,
            SensorCode: "BME280",
            SensorName: "BME280 Sensor",
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
            LastSeenAt: null,
            AssignedAt: DateTime.UtcNow,
            EffectiveConfig: new EffectiveConfigDto(
                IntervalSeconds: 60,
                I2CAddress: "0x76",
                SdaPin: 21,
                SclPin: 22,
                OneWirePin: null,
                AnalogPin: null,
                DigitalPin: null,
                TriggerPin: null,
                EchoPin: null,
                BaudRate: null,
                OffsetCorrection: 0,
                GainCorrection: 1
            )
        );
    }
}
