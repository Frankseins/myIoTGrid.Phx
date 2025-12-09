using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Node Debug Management (Sprint 8: Remote Debug System).
/// Provides endpoints for debug configuration, log retrieval, and error statistics.
/// Note: Main debug config endpoints (GET/PUT {id}/debug) are now in NodesController.
/// This controller handles by-serial endpoints and other debug operations.
/// </summary>
[ApiController]
[Route("api/node-debug")]
[Produces("application/json")]
public class NodeDebugController : ControllerBase
{
    private readonly INodeDebugLogService _debugLogService;
    private readonly INodeHardwareStatusService _hardwareStatusService;
    private readonly ILogger<NodeDebugController> _logger;

    public NodeDebugController(
        INodeDebugLogService debugLogService,
        INodeHardwareStatusService hardwareStatusService,
        ILogger<NodeDebugController> logger)
    {
        _debugLogService = debugLogService;
        _hardwareStatusService = hardwareStatusService;
        _logger = logger;
    }

    // === Debug Configuration ===

    /// <summary>
    /// Gets debug configuration for a node by ID.
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Debug configuration</returns>
    [HttpGet("{nodeId:guid}/debug")]
    [ProducesResponseType(typeof(NodeDebugConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDebugConfiguration(Guid nodeId, CancellationToken ct)
    {
        var config = await _debugLogService.GetDebugConfigurationAsync(nodeId, ct);
        if (config == null)
        {
            return NotFound(new { Message = $"Node {nodeId} not found" });
        }
        return Ok(config);
    }

    /// <summary>
    /// Gets debug configuration for a node by serial number (MAC address).
    /// Used by ESP32 firmware to check debug settings.
    /// </summary>
    /// <param name="serialNumber">Node serial number (MAC address)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Debug configuration</returns>
    [HttpGet("by-serial/{serialNumber}/debug")]
    [ProducesResponseType(typeof(NodeDebugConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDebugConfigurationBySerial(string serialNumber, CancellationToken ct)
    {
        var config = await _debugLogService.GetDebugConfigurationBySerialAsync(serialNumber, ct);
        if (config == null)
        {
            return NotFound(new { Message = $"Node with serial {serialNumber} not found" });
        }
        return Ok(config);
    }

    /// <summary>
    /// Sets debug level and remote logging for a node.
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="dto">Debug configuration</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Updated debug configuration</returns>
    [HttpPut("{nodeId:guid}/debug")]
    [ProducesResponseType(typeof(NodeDebugConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDebugLevel(Guid nodeId, [FromBody] SetNodeDebugLevelDto dto, CancellationToken ct)
    {
        var config = await _debugLogService.SetDebugLevelAsync(nodeId, dto, ct);
        if (config == null)
        {
            return NotFound(new { Message = $"Node {nodeId} not found" });
        }

        _logger.LogInformation("Set debug level for node {NodeId}: Level={Level}, RemoteLogging={RemoteLogging}",
            nodeId, dto.DebugLevel, dto.EnableRemoteLogging);

        return Ok(config);
    }

    /// <summary>
    /// Sets debug level for a node by serial number.
    /// </summary>
    /// <param name="serialNumber">Node serial number (MAC address)</param>
    /// <param name="dto">Debug configuration</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Updated debug configuration</returns>
    [HttpPut("by-serial/{serialNumber}/debug")]
    [ProducesResponseType(typeof(NodeDebugConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDebugLevelBySerial(string serialNumber, [FromBody] SetNodeDebugLevelDto dto, CancellationToken ct)
    {
        var config = await _debugLogService.SetDebugLevelBySerialAsync(serialNumber, dto, ct);
        if (config == null)
        {
            return NotFound(new { Message = $"Node with serial {serialNumber} not found" });
        }

        _logger.LogInformation("Set debug level for node {SerialNumber}: Level={Level}, RemoteLogging={RemoteLogging}",
            serialNumber, dto.DebugLevel, dto.EnableRemoteLogging);

        return Ok(config);
    }

    // === Debug Logs ===

    /// <summary>
    /// Gets debug logs for a node with filtering and paging.
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="filter">Filter options</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated debug logs</returns>
    [HttpGet("{nodeId:guid}/debug/logs")]
    [ProducesResponseType(typeof(PaginatedResultDto<NodeDebugLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(Guid nodeId, [FromQuery] DebugLogFilterDto filter, CancellationToken ct)
    {
        // Ensure nodeId is set in filter
        filter = filter with { NodeId = nodeId };
        var logs = await _debugLogService.GetLogsAsync(filter, ct);
        return Ok(logs);
    }

    /// <summary>
    /// Gets recent debug logs for a node (for live view).
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="count">Number of logs to retrieve (default: 50)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Recent debug logs</returns>
    [HttpGet("{nodeId:guid}/debug/logs/recent")]
    [ProducesResponseType(typeof(IEnumerable<NodeDebugLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentLogs(Guid nodeId, [FromQuery] int count = 50, CancellationToken ct = default)
    {
        var logs = await _debugLogService.GetRecentLogsAsync(nodeId, count, ct);
        return Ok(logs);
    }

    /// <summary>
    /// Receives a batch of debug logs from firmware.
    /// Called by ESP32 devices to upload logs.
    /// </summary>
    /// <param name="dto">Batch of debug logs</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Number of logs created</returns>
    [HttpPost("debug-logs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLogBatch([FromBody] DebugLogBatchDto dto, CancellationToken ct)
    {
        var count = await _debugLogService.CreateBatchAsync(dto.NodeId, dto.Logs, ct);

        if (count == 0)
        {
            // Could be node not found or remote logging disabled
            _logger.LogDebug("No logs created for node {NodeId} - node not found or remote logging disabled", dto.NodeId);
        }

        return Ok(new { Created = count });
    }

    /// <summary>
    /// Clears all debug logs for a node.
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Number of logs deleted</returns>
    [HttpDelete("{nodeId:guid}/debug/logs")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearLogs(Guid nodeId, CancellationToken ct)
    {
        var count = await _debugLogService.ClearLogsAsync(nodeId, ct);
        _logger.LogInformation("Cleared {Count} debug logs for node {NodeId}", count, nodeId);
        return Ok(new { Deleted = count });
    }

    // === Error Statistics ===

    /// <summary>
    /// Gets error statistics for a node.
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Error statistics</returns>
    [HttpGet("{nodeId:guid}/debug/statistics")]
    [ProducesResponseType(typeof(NodeErrorStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetErrorStatistics(Guid nodeId, CancellationToken ct)
    {
        var stats = await _debugLogService.GetErrorStatisticsAsync(nodeId, ct);
        if (stats == null)
        {
            return NotFound(new { Message = $"Node {nodeId} not found" });
        }
        return Ok(stats);
    }

    /// <summary>
    /// Gets error statistics for all nodes.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of error statistics</returns>
    [HttpGet("debug/statistics")]
    [ProducesResponseType(typeof(IEnumerable<NodeErrorStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllErrorStatistics(CancellationToken ct)
    {
        var stats = await _debugLogService.GetAllErrorStatisticsAsync(ct);
        return Ok(stats);
    }

    // === Cleanup ===

    /// <summary>
    /// Cleans up debug logs older than specified days.
    /// </summary>
    /// <param name="days">Number of days to keep (default: 7)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Cleanup result</returns>
    [HttpPost("debug/cleanup")]
    [ProducesResponseType(typeof(DebugLogCleanupResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupLogs([FromQuery] int days = 7, CancellationToken ct = default)
    {
        var before = DateTime.UtcNow.AddDays(-days);
        var result = await _debugLogService.CleanupLogsAsync(before, ct);
        _logger.LogInformation("Cleaned up {Count} debug logs older than {Before}", result.DeletedCount, before);
        return Ok(result);
    }

    // === Hardware Status (Sprint 8) ===

    /// <summary>
    /// Reports hardware status from firmware (called by ESP32 after boot).
    /// </summary>
    /// <param name="dto">Hardware status report</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Stored hardware status</returns>
    [HttpPost("hardware-status")]
    [ProducesResponseType(typeof(NodeHardwareStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReportHardwareStatus([FromBody] ReportHardwareStatusDto dto, CancellationToken ct)
    {
        var status = await _hardwareStatusService.ReportHardwareStatusAsync(dto, ct);
        if (status == null)
        {
            return NotFound(new { Message = $"Node with serial {dto.SerialNumber} not found" });
        }

        _logger.LogInformation("Hardware status reported for node {SerialNumber}: {DeviceCount} devices, SD={HasSd}",
            dto.SerialNumber, dto.DetectedDevices.Count, dto.Storage.Available);

        return Ok(status);
    }

    /// <summary>
    /// Gets hardware status for a node by ID.
    /// </summary>
    /// <param name="nodeId">Node ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Hardware status</returns>
    [HttpGet("{nodeId:guid}/hardware-status")]
    [ProducesResponseType(typeof(NodeHardwareStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHardwareStatus(Guid nodeId, CancellationToken ct)
    {
        var status = await _hardwareStatusService.GetHardwareStatusAsync(nodeId, ct);
        if (status == null)
        {
            return NotFound(new { Message = $"Node {nodeId} not found" });
        }
        return Ok(status);
    }

    /// <summary>
    /// Gets hardware status for a node by serial number (MAC address).
    /// </summary>
    /// <param name="serialNumber">Node serial number</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Hardware status</returns>
    [HttpGet("by-serial/{serialNumber}/hardware-status")]
    [ProducesResponseType(typeof(NodeHardwareStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHardwareStatusBySerial(string serialNumber, CancellationToken ct)
    {
        var status = await _hardwareStatusService.GetHardwareStatusBySerialAsync(serialNumber, ct);
        if (status == null)
        {
            return NotFound(new { Message = $"Node with serial {serialNumber} not found" });
        }
        return Ok(status);
    }

    // === Remote Serial Monitor (Sprint 8) ===

    /// <summary>
    /// Receives raw serial output from firmware (Remote Serial Monitor).
    /// Contains raw lines exactly as they appear in ESP32 serial monitor.
    /// </summary>
    /// <param name="dto">Batch of serial output lines</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Number of lines received</returns>
    [HttpPost("serial-output")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveSerialOutput([FromBody] SerialOutputBatchDto dto, CancellationToken ct)
    {
        if (dto.Lines.Count == 0)
        {
            return Ok(new { Received = 0 });
        }

        // Store lines as debug logs with "Serial" category
        var logs = dto.Lines.Select(line => new CreateNodeDebugLogDto
        {
            NodeTimestamp = dto.Timestamp,
            Level = DebugLevelDto.Debug,
            Category = LogCategoryDto.System,
            Message = line
        }).ToList();

        var count = await _debugLogService.CreateBatchAsync(dto.SerialNumber, logs, ct);

        _logger.LogDebug("Received {Count} serial lines from {SerialNumber}", dto.Lines.Count, dto.SerialNumber);

        return Ok(new { Received = count });
    }
}
