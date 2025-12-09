using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Readings (Measurements).
/// Matter-konform: Entspricht Matter Attribute Reports.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReadingsController : ControllerBase
{
    private readonly IReadingService _readingService;
    private readonly IChartService _chartService;

    public ReadingsController(IReadingService readingService, IChartService chartService)
    {
        _readingService = readingService;
        _chartService = chartService;
    }

    /// <summary>
    /// Creates a new Reading (Measurement)
    /// </summary>
    /// <param name="dto">Reading data from sensor</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Reading</returns>
    /// <response code="201">Reading successfully created</response>
    /// <response code="400">Invalid data</response>
    [HttpPost]
    [ProducesResponseType(typeof(ReadingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorReadingDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.DeviceId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "DeviceId is required"
            });
        }

        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Type is required"
            });
        }

        var reading = await _readingService.CreateFromSensorAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = reading.Id }, reading);
    }

    /// <summary>
    /// Creates multiple Readings in a single batch (Sprint OS-01: Offline Storage sync).
    /// Used by ESP32 sensors to upload locally stored readings when WiFi reconnects.
    /// </summary>
    /// <param name="dto">Batch data containing NodeId and list of readings</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Summary of batch upload result</returns>
    /// <response code="200">Batch processed (check SuccessCount/FailedCount)</response>
    /// <response code="400">Invalid request</response>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchReadingsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchReadingsDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.NodeId))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "NodeId is required"
            });
        }

        if (dto.Readings == null || !dto.Readings.Any())
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Readings list cannot be empty"
            });
        }

        var result = await _readingService.CreateBatchAsync(dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns Readings filtered and paginated (legacy)
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated list of Readings</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResultDto<ReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFiltered([FromQuery] ReadingFilterDto filter, CancellationToken ct)
    {
        var result = await _readingService.GetFilteredAsync(filter, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns Readings with server-side paging, sorting, and filtering
    /// </summary>
    /// <param name="queryParams">Query parameters (page, size, sort, search, filters)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated list of Readings</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResultDto<ReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] QueryParamsDto queryParams, CancellationToken ct)
    {
        var result = await _readingService.GetPagedAsync(queryParams, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a Reading by ID
    /// </summary>
    /// <param name="id">Reading-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Reading</returns>
    /// <response code="200">Reading found</response>
    /// <response code="404">Reading not found</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ReadingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var reading = await _readingService.GetByIdAsync(id, ct);

        if (reading == null)
            return NotFound();

        return Ok(reading);
    }

    /// <summary>
    /// Returns the latest Readings of all Nodes
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of latest Readings per Node and SensorType</returns>
    [HttpGet("latest")]
    [ProducesResponseType(typeof(IEnumerable<ReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var result = await _readingService.GetLatestAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the latest Readings of a Node
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of latest Readings per SensorType</returns>
    [HttpGet("latest/{nodeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestByNode(Guid nodeId, CancellationToken ct)
    {
        var result = await _readingService.GetLatestByNodeAsync(nodeId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns all Readings for a Node
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="filter">Optional filter criteria</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Readings</returns>
    [HttpGet("node/{nodeId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ReadingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByNode(Guid nodeId, [FromQuery] ReadingFilterDto? filter, CancellationToken ct)
    {
        var result = await _readingService.GetByNodeAsync(nodeId, filter, ct);
        return Ok(result);
    }

    // ==================== DELETE ENDPOINTS ====================

    /// <summary>
    /// Deletes readings within a date range.
    /// Supports optional filtering by sensor assignment and measurement type.
    /// </summary>
    /// <param name="dto">Delete criteria (nodeId, from, to, optional filters)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Number of deleted readings</returns>
    /// <response code="200">Readings successfully deleted</response>
    /// <response code="400">Invalid request parameters</response>
    [HttpDelete("range")]
    [ProducesResponseType(typeof(DeleteReadingsResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRange([FromBody] DeleteReadingsRangeDto dto, CancellationToken ct)
    {
        if (dto.NodeId == Guid.Empty)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "NodeId is required"
            });
        }

        if (dto.From > dto.To)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "From date must be before or equal to To date"
            });
        }

        var result = await _readingService.DeleteRangeAsync(dto, ct);
        return Ok(result);
    }

    // ==================== CHART ENDPOINTS ====================

    /// <summary>
    /// Returns chart data for a specific widget (node + assignment + measurement type)
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="assignmentId">Sensor assignment ID</param>
    /// <param name="measurementType">Measurement type (e.g., temperature, humidity)</param>
    /// <param name="interval">Time interval for aggregation</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Chart data with aggregated points, stats, and trend</returns>
    /// <response code="200">Chart data found</response>
    /// <response code="404">No data found for the specified parameters</response>
    [HttpGet("chart/{nodeId:guid}/{assignmentId:guid}/{measurementType}")]
    [ProducesResponseType(typeof(ChartDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChartData(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        [FromQuery] ChartInterval interval = ChartInterval.OneDay,
        CancellationToken ct = default)
    {
        var result = await _chartService.GetChartDataAsync(nodeId, assignmentId, measurementType, interval, ct);

        if (result == null)
            return NotFound(new ProblemDetails
            {
                Title = "No Data Found",
                Detail = $"No readings found for node {nodeId}, assignment {assignmentId}, type {measurementType}"
            });

        return Ok(result);
    }

    /// <summary>
    /// Returns paginated readings list for a specific widget
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="assignmentId">Sensor assignment ID</param>
    /// <param name="measurementType">Measurement type</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Page size (default 20)</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated list of readings</returns>
    [HttpGet("list/{nodeId:guid}/{assignmentId:guid}/{measurementType}")]
    [ProducesResponseType(typeof(ReadingsListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReadingsList(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var request = new ReadingsListRequestDto(page, pageSize, from, to);
        var result = await _chartService.GetReadingsListAsync(nodeId, assignmentId, measurementType, request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Exports readings to CSV file
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="assignmentId">Sensor assignment ID</param>
    /// <param name="measurementType">Measurement type</param>
    /// <param name="from">Optional start date filter</param>
    /// <param name="to">Optional end date filter</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>CSV file download</returns>
    [HttpGet("list/{nodeId:guid}/{assignmentId:guid}/{measurementType}/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToCsv(
        Guid nodeId,
        Guid assignmentId,
        string measurementType,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var csvData = await _chartService.ExportToCsvAsync(nodeId, assignmentId, measurementType, from, to, ct);
        var fileName = $"readings_{measurementType}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(csvData, "text/csv", fileName);
    }
}
