using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Unit tests for AlertTypesController
/// </summary>
public class AlertTypesControllerTests
{
    private readonly Mock<IAlertTypeService> _alertTypeServiceMock;
    private readonly AlertTypesController _sut;

    private readonly Guid _alertTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public AlertTypesControllerTests()
    {
        _alertTypeServiceMock = new Mock<IAlertTypeService>();
        _sut = new AlertTypesController(_alertTypeServiceMock.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAlertTypes()
    {
        // Arrange
        var alertTypes = new List<AlertTypeDto>
        {
            CreateAlertTypeDto("mold_risk", "Schimmelrisiko"),
            CreateAlertTypeDto("frost_warning", "Frostwarnung")
        };

        _alertTypeServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertTypes);

        // Act
        var result = await _sut.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(alertTypes);
    }

    [Fact]
    public async Task GetAll_WithEmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        _alertTypeServiceMock.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AlertTypeDto>());

        // Act
        var result = await _sut.GetAll(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var alertTypes = okResult.Value as IEnumerable<AlertTypeDto>;
        alertTypes.Should().BeEmpty();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingAlertType_ReturnsOk()
    {
        // Arrange
        var alertType = CreateAlertTypeDto("mold_risk", "Schimmelrisiko");

        _alertTypeServiceMock.Setup(s => s.GetByIdAsync(_alertTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertType);

        // Act
        var result = await _sut.GetById(_alertTypeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(alertType);
    }

    [Fact]
    public async Task GetById_WithNonExistingAlertType_ReturnsNotFound()
    {
        // Arrange
        _alertTypeServiceMock.Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertTypeDto?)null);

        // Act
        var result = await _sut.GetById(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetByCode Tests

    [Fact]
    public async Task GetByCode_WithExistingCode_ReturnsOk()
    {
        // Arrange
        var alertType = CreateAlertTypeDto("mold_risk", "Schimmelrisiko");

        _alertTypeServiceMock.Setup(s => s.GetByCodeAsync("mold_risk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertType);

        // Act
        var result = await _sut.GetByCode("mold_risk", CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(alertType);
    }

    [Fact]
    public async Task GetByCode_WithNonExistingCode_ReturnsNotFound()
    {
        // Arrange
        _alertTypeServiceMock.Setup(s => s.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AlertTypeDto?)null);

        // Act
        var result = await _sut.GetByCode("nonexistent", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateAlertTypeDto(
            Code: "custom_alert",
            Name: "Custom Alert",
            Description: "A custom alert type",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning"
        );

        var alertType = new AlertTypeDto(
            Id: _alertTypeId,
            Code: "custom_alert",
            Name: "Custom Alert",
            Description: "A custom alert type",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning",
            IsGlobal: false,
            CreatedAt: DateTime.UtcNow
        );

        _alertTypeServiceMock.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alertType);

        // Act
        var result = await _sut.Create(createDto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AlertTypesController.GetById));
        createdResult.Value.Should().BeEquivalentTo(alertType);
    }

    #endregion

    #region Helper Methods

    private AlertTypeDto CreateAlertTypeDto(string code, string name)
    {
        return new AlertTypeDto(
            Id: _alertTypeId,
            Code: code,
            Name: name,
            Description: $"Description for {name}",
            DefaultLevel: AlertLevelDto.Warning,
            IconName: "warning",
            IsGlobal: true,
            CreatedAt: DateTime.UtcNow
        );
    }

    #endregion
}
