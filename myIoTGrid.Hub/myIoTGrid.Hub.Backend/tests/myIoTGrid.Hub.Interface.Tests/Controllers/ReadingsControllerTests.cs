using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for ReadingsController.
/// New 3-tier model: Reading uses AssignmentId + MeasurementType instead of SensorTypeId.
/// </summary>
public class ReadingsControllerTests
{
    private readonly Mock<IReadingService> _readingServiceMock;
    private readonly ReadingsController _sut;

    private readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private readonly Guid _assignmentId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    public ReadingsControllerTests()
    {
        _readingServiceMock = new Mock<IReadingService>();
        _sut = new ReadingsController(_readingServiceMock.Object);
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateSensorReadingDto(
            DeviceId: "node-01",
            Type: "temperature",
            Value: 21.5,
            Timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
        var reading = CreateReadingDto(1, "temperature", 21.5);

        _readingServiceMock.Setup(s => s.CreateFromSensorAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ReadingsController.GetById));
        createdResult.Value.Should().BeOfType<ReadingDto>();
    }

    [Fact]
    public async Task Create_WithMinimalData_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateSensorReadingDto(
            DeviceId: "node-01",
            Type: "humidity",
            Value: 65.0
        );
        var reading = CreateReadingDto(2, "humidity", 65.0);

        _readingServiceMock.Setup(s => s.CreateFromSensorAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        // Act
        var result = await _sut.Create(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    #endregion

    #region GetFiltered Tests

    [Fact]
    public async Task GetFiltered_ReturnsOkWithPaginatedResult()
    {
        // Arrange
        var filter = new ReadingFilterDto(
            NodeId: _nodeId,
            MeasurementType: "temperature",
            Page: 1,
            PageSize: 10
        );
        var readings = new List<ReadingDto>
        {
            CreateReadingDto(1, "temperature", 21.5),
            CreateReadingDto(2, "temperature", 22.0)
        };
        var paginatedResult = new PaginatedResultDto<ReadingDto>(
            Items: readings,
            TotalCount: 2,
            Page: 1,
            PageSize: 10
        );

        _readingServiceMock.Setup(s => s.GetFilteredAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _sut.GetFiltered(filter, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<PaginatedResultDto<ReadingDto>>().Subject;
        returnedResult.TotalCount.Should().Be(2);
        returnedResult.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFiltered_WithEmptyFilter_ReturnsOk()
    {
        // Arrange
        var filter = new ReadingFilterDto();
        var paginatedResult = new PaginatedResultDto<ReadingDto>(
            Items: new List<ReadingDto>(),
            TotalCount: 0,
            Page: 1,
            PageSize: 10
        );

        _readingServiceMock.Setup(s => s.GetFilteredAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _sut.GetFiltered(filter, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WithExistingReading_ReturnsOkWithReading()
    {
        // Arrange
        var reading = CreateReadingDto(1, "temperature", 21.5);

        _readingServiceMock.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        // Act
        var result = await _sut.GetById(1, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReading = okResult.Value.Should().BeOfType<ReadingDto>().Subject;
        returnedReading.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithNonExistingReading_ReturnsNotFound()
    {
        // Arrange
        _readingServiceMock.Setup(s => s.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadingDto?)null);

        // Act
        var result = await _sut.GetById(999, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region GetLatest Tests

    [Fact]
    public async Task GetLatest_ReturnsOkWithReadings()
    {
        // Arrange
        var readings = new List<ReadingDto>
        {
            CreateReadingDto(1, "temperature", 21.5),
            CreateReadingDto(2, "humidity", 65.0)
        };

        _readingServiceMock.Setup(s => s.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetLatest(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<ReadingDto>>().Subject;
        returnedReadings.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetLatest_WithNoReadings_ReturnsOkWithEmptyList()
    {
        // Arrange
        _readingServiceMock.Setup(s => s.GetLatestAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadingDto>());

        // Act
        var result = await _sut.GetLatest(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<ReadingDto>>().Subject;
        returnedReadings.Should().BeEmpty();
    }

    #endregion

    #region GetLatestByNode Tests

    [Fact]
    public async Task GetLatestByNode_ReturnsOkWithReadings()
    {
        // Arrange
        var readings = new List<ReadingDto>
        {
            CreateReadingDto(1, "temperature", 21.5),
            CreateReadingDto(2, "humidity", 65.0)
        };

        _readingServiceMock.Setup(s => s.GetLatestByNodeAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetLatestByNode(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<ReadingDto>>().Subject;
        returnedReadings.Should().HaveCount(2);
    }

    #endregion

    #region GetByNode Tests

    [Fact]
    public async Task GetByNode_ReturnsOkWithReadings()
    {
        // Arrange
        var readings = new List<ReadingDto>
        {
            CreateReadingDto(1, "temperature", 21.5),
            CreateReadingDto(2, "temperature", 22.0),
            CreateReadingDto(3, "temperature", 21.8)
        };

        _readingServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetByNode(_nodeId, null, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedReadings = okResult.Value.Should().BeAssignableTo<IEnumerable<ReadingDto>>().Subject;
        returnedReadings.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByNode_WithFilter_PassesFilterToService()
    {
        // Arrange
        var filter = new ReadingFilterDto(
            MeasurementType: "temperature",
            From: DateTime.UtcNow.AddDays(-1),
            To: DateTime.UtcNow
        );
        var readings = new List<ReadingDto> { CreateReadingDto(1, "temperature", 21.5) };

        _readingServiceMock.Setup(s => s.GetByNodeAsync(_nodeId, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetByNode(_nodeId, filter, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _readingServiceMock.Verify(s => s.GetByNodeAsync(_nodeId, filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private ReadingDto CreateReadingDto(long id, string measurementType, double value)
    {
        return new ReadingDto(
            Id: id,
            TenantId: _tenantId,
            NodeId: _nodeId,
            NodeName: "Test Node",
            AssignmentId: _assignmentId,
            MeasurementType: measurementType,
            RawValue: value,
            Value: value,
            Unit: measurementType == "temperature" ? "°C" : "%",
            Timestamp: DateTime.UtcNow,
            Location: null,
            IsSyncedToCloud: false
        );
    }

    #endregion
}
