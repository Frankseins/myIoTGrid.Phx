using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Sensors (Physical sensor chips: DHT22, BME280).
/// Matter-konform: Entspricht Matter Endpoints.
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
    /// Returns all Sensors for a Node
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Sensors</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SensorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid nodeId, CancellationToken ct)
    {
        var sensors = await _sensorService.GetByNodeAsync(nodeId, ct);
        return Ok(sensors);
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
    /// Creates a new Sensor on a Node
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="dto">Sensor data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Sensor</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromQuery] Guid nodeId, [FromBody] CreateSensorDto dto, CancellationToken ct)
    {
        try
        {
            var sensor = await _sensorService.CreateAsync(nodeId, dto, ct);
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSensorDto dto, CancellationToken ct)
    {
        var sensor = await _sensorService.UpdateAsync(id, dto, ct);

        if (sensor == null)
            return NotFound();

        return Ok(sensor);
    }

    /// <summary>
    /// Deletes a Sensor
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _sensorService.DeleteAsync(id, ct);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Syncs sensors for a Node based on sensor type IDs
    /// </summary>
    /// <param name="nodeId">Node-ID</param>
    /// <param name="sensorTypeIds">List of sensor type IDs</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>All Sensors for the Node</returns>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(IEnumerable<SensorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sync([FromQuery] Guid nodeId, [FromBody] IEnumerable<string> sensorTypeIds, CancellationToken ct)
    {
        var sensors = await _sensorService.SyncSensorsAsync(nodeId, sensorTypeIds, ct);
        return Ok(sensors);
    }
}
