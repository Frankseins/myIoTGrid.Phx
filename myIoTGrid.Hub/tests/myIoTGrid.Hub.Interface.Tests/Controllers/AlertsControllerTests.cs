using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for AlertsController.
/// Alerts werden von der Cloud-KI gesendet oder lokal generiert.
/// </summary>
public class AlertsControllerTests
{
    private readonly Mock<IAlertService> _alertServiceMock;
    private readonly AlertsController _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _alertId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _hubId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private readonly Guid _alertTypeId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    public AlertsControllerTests()
    {
        _alertServiceMock = new Mock<IAlertService>();
        _sut = new AlertsController(_alertServiceMock.Object);
    }

    #region GetActive Tests

    [Fact]
    public async Task GetActive_ReturnsOkWithAlerts()
    {
        // Arrange
        var alerts = new List<AlertDto>
        {
            CreateAlertDto("high_temperature", AlertLevelDto.Warning),
            CreateAlertDto("low_battery", AlertLevelDto.Info)
        };

        _alertServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        // Act
        var result = await _sut.GetActive(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlerts = okResult.Value.Should().BeAssignableTo<IEnumerable<AlertDto>>().Subject;
        returnedAlerts.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActive_WithNoAlerts_ReturnsOkWithEmptyList()
    {
        // Arrange
        _alertServiceMock.Setup(s => s.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlertDto>());

        // Act
        var result = await _sut.GetActive(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlerts = okResult.Value.Should().BeAssignableTo<IEnumerable<AlertDto>>().Subject;
        returnedAlerts.Should().BeEmpty();
    }

    #endregion

    #region GetFiltered Tests

    [Fact]
    public async Task GetFiltered_ReturnsOkWithPaginatedResult()
    {
        // Arrange
        var filter = new AlertFilterDto(
            Level: AlertLevelDto.Warning,
            IsActive: true,
            Page: 1,
            PageSize: 10
        );
        var alerts = new List<AlertDto>
        {
            CreateAlertDto("high_temperature", AlertLevelDto.Warning)
        };
        var paginatedResult = new PaginatedResultDto<AlertDto>(
            Items: alerts,
            TotalCount: 1,
            Page: 1,
            PageSize: 10
        );

        _alertServiceMock.Setup(s => s.GetFilteredAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _sut.GetFiltered(filter, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<PaginatedResultDto<AlertDto>>().Subject;
        returnedResult.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetFiltered_WithEmptyFilter_ReturnsOk()
    {
        // Arrange
        var filter = new AlertFilterDto();
        var paginatedResult = new PaginatedResultDto<AlertDto>(
            Items: new List<AlertDto>(),
            TotalCount: 0,
            Page: 1,
            PageSize: 10
        );

        _alertServiceMock.Setup(s => s.GetFilteredAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _sut.GetFiltered(filter, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingAlert_ReturnsOkWithAlert()
    {
        // Arrange
        var alert = CreateAlertDto("high_temperature", AlertLevelDto.Warning);

        _alertServiceMock.Setup(s => s.GetByIdAsync(_alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        // Act
        var result = await _sut.GetById(_alertId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlert = okResult.Value.Should().BeOfType<AlertDto>().Subject;
        returnedAlert.AlertTypeCode.Should().Be("high_temperature");
    }

    [Fact]
    public async Task GetById_WithNonExistingAlert_ReturnsNotFound()
    {
        // Arrange
        _alertServiceMock.Setup(s => s.GetByIdAsync(_alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertDto?)null);

        // Act
        var result = await _sut.GetById(_alertId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Acknowledge Tests

    [Fact]
    public async Task Acknowledge_WithExistingAlert_ReturnsOkWithAcknowledgedAlert()
    {
        // Arrange
        var alert = CreateAlertDto("high_temperature", AlertLevelDto.Warning) with
        {
            AcknowledgedAt = DateTime.UtcNow,
            IsActive = false
        };

        _alertServiceMock.Setup(s => s.AcknowledgeAsync(_alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        // Act
        var result = await _sut.Acknowledge(_alertId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedAlert = okResult.Value.Should().BeOfType<AlertDto>().Subject;
        returnedAlert.AcknowledgedAt.Should().NotBeNull();
        returnedAlert.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Acknowledge_WithNonExistingAlert_ReturnsNotFound()
    {
        // Arrange
        _alertServiceMock.Setup(s => s.AcknowledgeAsync(_alertId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertDto?)null);

        // Act
        var result = await _sut.Acknowledge(_alertId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region ReceiveFromCloud Tests

    [Fact]
    public async Task ReceiveFromCloud_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "mold_risk",
            HubId: "hub-01",
            NodeId: "node-01",
            Level: AlertLevelDto.Warning,
            Message: "Schimmelgefahr erkannt",
            Recommendation: "Bitte lüften"
        );
        var alert = CreateAlertDto("mold_risk", AlertLevelDto.Warning);

        _alertServiceMock.Setup(s => s.CreateFromCloudAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        // Act
        var result = await _sut.ReceiveFromCloud(dto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AlertsController.GetById));
        createdResult.Value.Should().BeOfType<AlertDto>();
    }

    [Fact]
    public async Task ReceiveFromCloud_WithCriticalLevel_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateAlertDto(
            AlertTypeCode: "frost_warning",
            Level: AlertLevelDto.Critical,
            Message: "Frostgefahr! Temperatur unter 0°C"
        );
        var alert = CreateAlertDto("frost_warning", AlertLevelDto.Critical);

        _alertServiceMock.Setup(s => s.CreateFromCloudAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        // Act
        var result = await _sut.ReceiveFromCloud(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task ReceiveFromCloud_WithoutNodeId_ReturnsCreatedAtAction()
    {
        // Arrange - System-wide alert without specific node
        var dto = new CreateAlertDto(
            AlertTypeCode: "system_update",
            Level: AlertLevelDto.Info,
            Message: "System-Update verfügbar"
        );
        var alert = CreateAlertDto("system_update", AlertLevelDto.Info) with { NodeId = null };

        _alertServiceMock.Setup(s => s.CreateFromCloudAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alert);

        // Act
        var result = await _sut.ReceiveFromCloud(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    #endregion

    #region Helper Methods

    private AlertDto CreateAlertDto(string alertTypeCode, AlertLevelDto level)
    {
        return new AlertDto(
            Id: _alertId,
            TenantId: _tenantId,
            HubId: _hubId,
            HubName: "Test Hub",
            NodeId: _nodeId,
            NodeName: "Test Node",
            AlertTypeId: _alertTypeId,
            AlertTypeCode: alertTypeCode,
            AlertTypeName: alertTypeCode switch
            {
                "high_temperature" => "Hohe Temperatur",
                "low_battery" => "Niedriger Akkustand",
                "mold_risk" => "Schimmelrisiko",
                "frost_warning" => "Frostwarnung",
                _ => alertTypeCode
            },
            Level: level,
            Message: $"Alert: {alertTypeCode}",
            Recommendation: null,
            Source: AlertSourceDto.Cloud,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: null,
            AcknowledgedAt: null,
            IsActive: true
        );
    }

    #endregion
}
