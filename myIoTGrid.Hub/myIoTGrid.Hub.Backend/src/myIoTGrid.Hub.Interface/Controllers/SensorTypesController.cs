using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for SensorTypes (Hardware Library).
/// Global sensor definitions with capabilities and default configurations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SensorTypesController : ControllerBase
{
    private readonly ISensorTypeService _sensorTypeService;

    public SensorTypesController(ISensorTypeService sensorTypeService)
    {
        _sensorTypeService = sensorTypeService;
    }

    /// <summary>
    /// Returns all SensorTypes (cached)
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of all SensorTypes</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SensorTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sensorTypes = await _sensorTypeService.GetAllCachedAsync(ct);
        return Ok(sensorTypes);
    }

    /// <summary>
    /// Returns SensorTypes with server-side paging, sorting, and filtering
    /// </summary>
    /// <param name="queryParams">Query parameters (page, size, sort, search, filters)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated list of SensorTypes</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResultDto<SensorTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] QueryParamsDto queryParams, CancellationToken ct)
    {
        var result = await _sensorTypeService.GetPagedAsync(queryParams, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a SensorType by Id
    /// </summary>
    /// <param name="id">SensorType-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The SensorType</returns>
    /// <response code="200">SensorType found</response>
    /// <response code="404">SensorType not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sensorType = await _sensorTypeService.GetByIdAsync(id, ct);

        if (sensorType == null)
            return NotFound();

        return Ok(sensorType);
    }

    /// <summary>
    /// Returns a SensorType by Code
    /// </summary>
    /// <param name="code">SensorType Code (e.g. "dht22", "bme280")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The SensorType</returns>
    /// <response code="200">SensorType found</response>
    /// <response code="404">SensorType not found</response>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var sensorType = await _sensorTypeService.GetByCodeAsync(code, ct);

        if (sensorType == null)
            return NotFound();

        return Ok(sensorType);
    }

    /// <summary>
    /// Returns SensorTypes by Category
    /// </summary>
    /// <param name="category">Category (e.g. "climate", "water", "location")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of SensorTypes in category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<SensorTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var sensorTypes = await _sensorTypeService.GetByCategoryAsync(category, ct);
        return Ok(sensorTypes);
    }

    /// <summary>
    /// Returns all Capabilities for a SensorType
    /// </summary>
    /// <param name="id">SensorType-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Capabilities</returns>
    [HttpGet("{id:guid}/capabilities")]
    [ProducesResponseType(typeof(IEnumerable<SensorTypeCapabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilities(Guid id, CancellationToken ct)
    {
        var capabilities = await _sensorTypeService.GetCapabilitiesAsync(id, ct);
        return Ok(capabilities);
    }

    /// <summary>
    /// Creates a new custom SensorType
    /// </summary>
    /// <param name="dto">SensorType data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created SensorType</returns>
    /// <response code="201">SensorType successfully created</response>
    /// <response code="400">Invalid data or Code already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorTypeDto dto, CancellationToken ct)
    {
        try
        {
            var sensorType = await _sensorTypeService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = sensorType.Id }, sensorType);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Updates a SensorType
    /// </summary>
    /// <param name="id">SensorType-ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated SensorType</returns>
    /// <response code="200">SensorType successfully updated</response>
    /// <response code="400">Invalid data</response>
    /// <response code="404">SensorType not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSensorTypeDto dto, CancellationToken ct)
    {
        try
        {
            var sensorType = await _sensorTypeService.UpdateAsync(id, dto, ct);
            return Ok(sensorType);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Deletes a SensorType
    /// </summary>
    /// <param name="id">SensorType-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">SensorType successfully deleted</response>
    /// <response code="400">Cannot delete global SensorType</response>
    /// <response code="404">SensorType not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _sensorTypeService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("global"))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Triggers synchronization with Cloud
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpPost("sync")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SyncFromCloud(CancellationToken ct)
    {
        await _sensorTypeService.SyncFromCloudAsync(ct);
        return NoContent();
    }
}
