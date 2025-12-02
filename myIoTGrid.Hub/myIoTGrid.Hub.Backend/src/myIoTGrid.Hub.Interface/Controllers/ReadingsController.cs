using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;

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

    public ReadingsController(IReadingService readingService)
    {
        _readingService = readingService;
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
}
