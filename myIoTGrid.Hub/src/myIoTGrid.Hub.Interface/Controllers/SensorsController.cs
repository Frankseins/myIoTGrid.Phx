using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Sensors (v3.0).
/// Complete sensor definition with hardware configuration and calibration.
/// Two-tier model: Sensor → NodeSensorAssignment
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _sensorService;

    public SensorsController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    /// <summary>
    /// Returns all Sensors for the current tenant
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Sensors</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SensorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var sensors = await _sensorService.GetAllAsync(ct);
        return Ok(sensors);
    }

    /// <summary>
    /// Returns Sensors with server-side paging, sorting, and filtering
    /// </summary>
    /// <param name="queryParams">Query parameters (page, size, sort, search, filters)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Paginated list of Sensors</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(PagedResultDto<SensorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] QueryParamsDto queryParams, CancellationToken ct)
    {
        var result = await _sensorService.GetPagedAsync(queryParams, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a Sensor by ID
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Sensor</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var sensor = await _sensorService.GetByIdAsync(id, ct);

        if (sensor == null)
            return NotFound();

        return Ok(sensor);
    }

    /// <summary>
    /// Returns a Sensor by Code
    /// </summary>
    /// <param name="code">Sensor Code (e.g. "dht22", "bme280")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Sensor</returns>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken ct)
    {
        var sensor = await _sensorService.GetByCodeAsync(code, ct);

        if (sensor == null)
            return NotFound();

        return Ok(sensor);
    }

    /// <summary>
    /// Returns Sensors by Category
    /// </summary>
    /// <param name="category">Category (e.g. "climate", "water", "location")</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Sensors in category</returns>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<SensorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(string category, CancellationToken ct)
    {
        var sensors = await _sensorService.GetByCategoryAsync(category, ct);
        return Ok(sensors);
    }

    /// <summary>
    /// Returns all Capabilities for a Sensor
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Capabilities</returns>
    [HttpGet("{id:guid}/capabilities")]
    [ProducesResponseType(typeof(IEnumerable<SensorCapabilityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCapabilities(Guid id, CancellationToken ct)
    {
        var capabilities = await _sensorService.GetCapabilitiesAsync(id, ct);
        return Ok(capabilities);
    }

    /// <summary>
    /// Creates a new Sensor
    /// </summary>
    /// <param name="dto">Sensor data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Sensor</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorDto dto, CancellationToken ct)
    {
        try
        {
            var sensor = await _sensorService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = sensor.Id }, sensor);
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
    /// Updates a Sensor
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Sensor</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSensorDto dto, CancellationToken ct)
    {
        try
        {
            var sensor = await _sensorService.UpdateAsync(id, dto, ct);
            return Ok(sensor);
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
    /// Calibrates a Sensor with offset and gain corrections
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="dto">Calibration data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The calibrated Sensor</returns>
    [HttpPost("{id:guid}/calibrate")]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calibrate(Guid id, [FromBody] CalibrateSensorDto dto, CancellationToken ct)
    {
        try
        {
            var sensor = await _sensorService.CalibrateAsync(id, dto, ct);
            return Ok(sensor);
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
    /// Deletes a Sensor
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _sensorService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("assignment"))
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
    /// Seeds default sensors (standard templates like BME280, DHT22, etc.)
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SeedDefaultSensors(CancellationToken ct)
    {
        await _sensorService.SeedDefaultSensorsAsync(ct);
        return NoContent();
    }
}
