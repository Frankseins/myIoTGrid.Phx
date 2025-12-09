using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using myIoTGrid.Hub.Interface.Controllers;

namespace myIoTGrid.Hub.Interface.Tests.Controllers;

/// <summary>
/// Tests for NodeDebugController (Sprint 8: Remote Debug System).
/// Tests debug configuration, log retrieval, error statistics, and hardware status.
/// </summary>
public class NodeDebugControllerTests
{
    private readonly Mock<INodeDebugLogService> _debugLogServiceMock;
    private readonly Mock<INodeHardwareStatusService> _hardwareStatusServiceMock;
    private readonly Mock<ILogger<NodeDebugController>> _loggerMock;
    private readonly NodeDebugController _sut;

    private readonly Guid _nodeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly string _serialNumber = "AA:BB:CC:DD:EE:FF";

    public NodeDebugControllerTests()
    {
        _debugLogServiceMock = new Mock<INodeDebugLogService>();
        _hardwareStatusServiceMock = new Mock<INodeHardwareStatusService>();
        _loggerMock = new Mock<ILogger<NodeDebugController>>();
        _sut = new NodeDebugController(
            _debugLogServiceMock.Object,
            _hardwareStatusServiceMock.Object,
            _loggerMock.Object);
    }

    #region GetDebugConfiguration Tests

    [Fact]
    public async Task GetDebugConfiguration_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var config = CreateDebugConfigurationDto();
        _debugLogServiceMock.Setup(s => s.GetDebugConfigurationAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.GetDebugConfiguration(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(config);
    }

    [Fact]
    public async Task GetDebugConfiguration_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        _debugLogServiceMock.Setup(s => s.GetDebugConfigurationAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDebugConfigurationDto?)null);

        // Act
        var result = await _sut.GetDebugConfiguration(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetDebugConfigurationBySerial Tests

    [Fact]
    public async Task GetDebugConfigurationBySerial_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var config = CreateDebugConfigurationDto();
        _debugLogServiceMock.Setup(s => s.GetDebugConfigurationBySerialAsync(_serialNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.GetDebugConfigurationBySerial(_serialNumber, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(config);
    }

    [Fact]
    public async Task GetDebugConfigurationBySerial_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        _debugLogServiceMock.Setup(s => s.GetDebugConfigurationBySerialAsync(_serialNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDebugConfigurationDto?)null);

        // Act
        var result = await _sut.GetDebugConfigurationBySerial(_serialNumber, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region SetDebugLevel Tests

    [Fact]
    public async Task SetDebugLevel_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Debug, true);
        var config = CreateDebugConfigurationDto();
        _debugLogServiceMock.Setup(s => s.SetDebugLevelAsync(_nodeId, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.SetDebugLevel(_nodeId, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetDebugLevel_WhenNodeNotFound_ReturnsNotFound()
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

    #region SetDebugLevelBySerial Tests

    [Fact]
    public async Task SetDebugLevelBySerial_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Normal, false);
        var config = CreateDebugConfigurationDto();
        _debugLogServiceMock.Setup(s => s.SetDebugLevelBySerialAsync(_serialNumber, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // Act
        var result = await _sut.SetDebugLevelBySerial(_serialNumber, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task SetDebugLevelBySerial_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = new SetNodeDebugLevelDto(DebugLevelDto.Normal, false);
        _debugLogServiceMock.Setup(s => s.SetDebugLevelBySerialAsync(_serialNumber, dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeDebugConfigurationDto?)null);

        // Act
        var result = await _sut.SetDebugLevelBySerial(_serialNumber, dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetLogs Tests

    [Fact]
    public async Task GetLogs_ReturnsOkWithPaginatedResult()
    {
        // Arrange
        var filter = new DebugLogFilterDto(_nodeId);
        var logs = new List<NodeDebugLogDto>
        {
            new(_nodeId, _nodeId, 1000, DateTime.UtcNow, DebugLevelDto.Normal, LogCategoryDto.System, "Test log", null)
        };
        var paginatedResult = new PaginatedResultDto<NodeDebugLogDto>(logs, 1, 1, 100);
        _debugLogServiceMock.Setup(s => s.GetLogsAsync(It.IsAny<DebugLogFilterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _sut.GetLogs(_nodeId, filter, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeOfType<PaginatedResultDto<NodeDebugLogDto>>().Subject;
        returned.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetLogs_SetsNodeIdInFilter()
    {
        // Arrange
        var filter = new DebugLogFilterDto(Guid.Empty); // Different NodeId
        DebugLogFilterDto? capturedFilter = null;
        _debugLogServiceMock.Setup(s => s.GetLogsAsync(It.IsAny<DebugLogFilterDto>(), It.IsAny<CancellationToken>()))
            .Callback<DebugLogFilterDto, CancellationToken>((f, _) => capturedFilter = f)
            .ReturnsAsync(new PaginatedResultDto<NodeDebugLogDto>(new List<NodeDebugLogDto>(), 0, 1, 100));

        // Act
        await _sut.GetLogs(_nodeId, filter, CancellationToken.None);

        // Assert
        capturedFilter.Should().NotBeNull();
        capturedFilter!.NodeId.Should().Be(_nodeId);
    }

    #endregion

    #region GetRecentLogs Tests

    [Fact]
    public async Task GetRecentLogs_ReturnsOkWithLogs()
    {
        // Arrange
        var logs = new List<NodeDebugLogDto>
        {
            new(_nodeId, _nodeId, 1000, DateTime.UtcNow, DebugLevelDto.Normal, LogCategoryDto.System, "Test log", null)
        };
        _debugLogServiceMock.Setup(s => s.GetRecentLogsAsync(_nodeId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _sut.GetRecentLogs(_nodeId, 50, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeDebugLogDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetRecentLogs_WithCustomCount_PassesToService()
    {
        // Arrange
        _debugLogServiceMock.Setup(s => s.GetRecentLogsAsync(_nodeId, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NodeDebugLogDto>());

        // Act
        await _sut.GetRecentLogs(_nodeId, 100, CancellationToken.None);

        // Assert
        _debugLogServiceMock.Verify(s => s.GetRecentLogsAsync(_nodeId, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateLogBatch Tests

    [Fact]
    public async Task CreateLogBatch_ReturnsOkWithCreatedCount()
    {
        // Arrange
        var dto = new DebugLogBatchDto
        {
            NodeId = _serialNumber,
            Logs = new List<CreateNodeDebugLogDto>
            {
                new() { NodeTimestamp = 1000, Level = DebugLevelDto.Normal, Category = LogCategoryDto.System, Message = "Test" }
            }
        };
        _debugLogServiceMock.Setup(s => s.CreateBatchAsync(_serialNumber, dto.Logs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.CreateLogBatch(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateLogBatch_WhenNoLogsCreated_ReturnsOkWithZero()
    {
        // Arrange
        var dto = new DebugLogBatchDto
        {
            NodeId = _serialNumber,
            Logs = new List<CreateNodeDebugLogDto>()
        };
        _debugLogServiceMock.Setup(s => s.CreateBatchAsync(_serialNumber, dto.Logs, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _sut.CreateLogBatch(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region ClearLogs Tests

    [Fact]
    public async Task ClearLogs_ReturnsOkWithDeletedCount()
    {
        // Arrange
        _debugLogServiceMock.Setup(s => s.ClearLogsAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _sut.ClearLogs(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetErrorStatistics Tests

    [Fact]
    public async Task GetErrorStatistics_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var stats = new NodeErrorStatisticsDto(
            _nodeId, "Node 1", 100, 5, 10, 85,
            new Dictionary<string, int> { { "System", 3 }, { "Network", 2 } },
            DateTime.UtcNow, "Last error message");
        _debugLogServiceMock.Setup(s => s.GetErrorStatisticsAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _sut.GetErrorStatistics(_nodeId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(stats);
    }

    [Fact]
    public async Task GetErrorStatistics_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        _debugLogServiceMock.Setup(s => s.GetErrorStatisticsAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeErrorStatisticsDto?)null);

        // Act
        var result = await _sut.GetErrorStatistics(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetAllErrorStatistics Tests

    [Fact]
    public async Task GetAllErrorStatistics_ReturnsOkWithStats()
    {
        // Arrange
        var stats = new List<NodeErrorStatisticsDto>
        {
            new(_nodeId, "Node 1", 100, 5, 10, 85, new Dictionary<string, int>(), DateTime.UtcNow, null)
        };
        _debugLogServiceMock.Setup(s => s.GetAllErrorStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _sut.GetAllErrorStatistics(CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<IEnumerable<NodeErrorStatisticsDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    #endregion

    #region CleanupLogs Tests

    [Fact]
    public async Task CleanupLogs_ReturnsOkWithResult()
    {
        // Arrange
        var cleanupResult = new DebugLogCleanupResultDto(50, DateTime.UtcNow.AddDays(-7));
        _debugLogServiceMock.Setup(s => s.CleanupLogsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cleanupResult);

        // Act
        var result = await _sut.CleanupLogs(7, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(cleanupResult);
    }

    [Fact]
    public async Task CleanupLogs_WithCustomDays_PassesToService()
    {
        // Arrange
        var cleanupResult = new DebugLogCleanupResultDto(0, DateTime.UtcNow.AddDays(-14));
        _debugLogServiceMock.Setup(s => s.CleanupLogsAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cleanupResult);

        // Act
        await _sut.CleanupLogs(14, CancellationToken.None);

        // Assert
        _debugLogServiceMock.Verify(s => s.CleanupLogsAsync(It.Is<DateTime>(d => d < DateTime.UtcNow.AddDays(-13)), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ReportHardwareStatus Tests

    [Fact]
    public async Task ReportHardwareStatus_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var dto = CreateReportHardwareStatusDto();
        var status = CreateNodeHardwareStatusDto();
        _hardwareStatusServiceMock.Setup(s => s.ReportHardwareStatusAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.ReportHardwareStatus(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ReportHardwareStatus_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        var dto = CreateReportHardwareStatusDto();
        _hardwareStatusServiceMock.Setup(s => s.ReportHardwareStatusAsync(dto, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeHardwareStatusDto?)null);

        // Act
        var result = await _sut.ReportHardwareStatus(dto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetHardwareStatus Tests

    [Fact]
    public async Task GetHardwareStatus_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var status = CreateNodeHardwareStatusDto();
        _hardwareStatusServiceMock.Setup(s => s.GetHardwareStatusAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.GetHardwareStatus(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHardwareStatus_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        _hardwareStatusServiceMock.Setup(s => s.GetHardwareStatusAsync(_nodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeHardwareStatusDto?)null);

        // Act
        var result = await _sut.GetHardwareStatus(_nodeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetHardwareStatusBySerial Tests

    [Fact]
    public async Task GetHardwareStatusBySerial_WhenNodeExists_ReturnsOk()
    {
        // Arrange
        var status = CreateNodeHardwareStatusDto();
        _hardwareStatusServiceMock.Setup(s => s.GetHardwareStatusBySerialAsync(_serialNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var result = await _sut.GetHardwareStatusBySerial(_serialNumber, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetHardwareStatusBySerial_WhenNodeNotFound_ReturnsNotFound()
    {
        // Arrange
        _hardwareStatusServiceMock.Setup(s => s.GetHardwareStatusBySerialAsync(_serialNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NodeHardwareStatusDto?)null);

        // Act
        var result = await _sut.GetHardwareStatusBySerial(_serialNumber, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helper Methods

    private NodeDebugConfigurationDto CreateDebugConfigurationDto()
    {
        return new NodeDebugConfigurationDto(
            _nodeId,
            _serialNumber,
            DebugLevelDto.Normal,
            true,
            DateTime.UtcNow);
    }

    private ReportHardwareStatusDto CreateReportHardwareStatusDto()
    {
        return new ReportHardwareStatusDto(
            SerialNumber: _serialNumber,
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32-S3",
            DetectedDevices: new List<DetectedDeviceDto>
            {
                new("BME280", "I2C", "0x76", "OK", "temperature", 1, null)
            },
            Storage: new StorageStatusDto(true, "LOCAL_AND_REMOTE", 1073741824, 104857600, 968884224, 0, null, null),
            BusStatus: new BusStatusDto(true, 2, new List<string> { "0x76", "0x77" }, true, 1, false, false));
    }

    private NodeHardwareStatusDto CreateNodeHardwareStatusDto()
    {
        return new NodeHardwareStatusDto(
            NodeId: _nodeId,
            SerialNumber: _serialNumber,
            FirmwareVersion: "1.0.0",
            HardwareType: "ESP32-S3",
            ReportedAt: DateTime.UtcNow,
            Summary: new HardwareSummaryDto(2, 1, 1, 0, true, false, "OK"),
            DetectedDevices: new List<DetectedDeviceDto>(),
            Storage: new StorageStatusDto(true, "LOCAL_AND_REMOTE", 1073741824, 104857600, 968884224, 0, null, null),
            BusStatus: new BusStatusDto(true, 2, new List<string> { "0x76" }, true, 1, false, false));
    }

    #endregion
}
