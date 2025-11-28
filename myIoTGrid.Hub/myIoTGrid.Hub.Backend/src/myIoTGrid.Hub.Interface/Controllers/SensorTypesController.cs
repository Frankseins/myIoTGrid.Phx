using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Sensor Types.
/// Matter-konform: Entspricht Matter Clusters.
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
    /// Returns all Sensor Types (cached)
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of all Sensor Types</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SensorTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sensorTypes = await _sensorTypeService.GetAllCachedAsync(ct);
        return Ok(sensorTypes);
    }

    /// <summary>
    /// Returns a Sensor Type by TypeId
    /// </summary>
    /// <param name="typeId">SensorType-TypeId (e.g. "temperature")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Sensor Type</returns>
    /// <response code="200">Sensor Type found</response>
    /// <response code="404">Sensor Type not found</response>
    [HttpGet("{typeId}")]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByTypeId(string typeId, CancellationToken ct)
    {
        var sensorType = await _sensorTypeService.GetByTypeIdAsync(typeId, ct);

        if (sensorType == null)
            return NotFound();

        return Ok(sensorType);
    }

    /// <summary>
    /// Returns Sensor Types by Category
    /// </summary>
    /// <param name="category">Category (e.g. "climate", "air_quality")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Sensor Types in category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<SensorTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var sensorTypes = await _sensorTypeService.GetByCategoryAsync(category, ct);
        return Ok(sensorTypes);
    }

    /// <summary>
    /// Returns the unit for a Sensor Type
    /// </summary>
    /// <param name="typeId">SensorType-TypeId</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The unit string</returns>
    [HttpGet("{typeId}/unit")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnit(string typeId, CancellationToken ct)
    {
        var unit = await _sensorTypeService.GetUnitAsync(typeId, ct);
        return Ok(unit);
    }

    /// <summary>
    /// Creates a new custom Sensor Type
    /// </summary>
    /// <param name="dto">Sensor Type data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Sensor Type</returns>
    /// <response code="201">Sensor Type successfully created</response>
    /// <response code="400">Invalid data or TypeId already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(SensorTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorTypeDto dto, CancellationToken ct)
    {
        try
        {
            var sensorType = await _sensorTypeService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetByTypeId), new { typeId = sensorType.TypeId }, sensorType);
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
}
