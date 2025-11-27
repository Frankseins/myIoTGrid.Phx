using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using myIoTGrid.Hub.Interface.Hubs;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Sensors (ESP32/LoRa32 Devices)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _sensorService;
    private readonly IHubService _hubService;
    private readonly IHubContext<SensorHub> _hubContext;

    public SensorsController(
        ISensorService sensorService,
        IHubService hubService,
        IHubContext<SensorHub> hubContext)
    {
        _sensorService = sensorService;
        _hubService = hubService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Returns a Sensor by ID
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Sensor</returns>
    /// <response code="200">Sensor found</response>
    /// <response code="404">Sensor not found</response>
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
    /// Creates a new Sensor
    /// </summary>
    /// <param name="dto">Sensor data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Sensor</returns>
    /// <response code="201">Sensor successfully created</response>
    /// <response code="400">Invalid data or SensorId already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSensorDto dto, CancellationToken ct)
    {
        // If HubId is provided as string, look it up
        var hubId = dto.HubId;
        if (hubId == null && !string.IsNullOrWhiteSpace(dto.HubIdentifier))
        {
            var hub = await _hubService.GetOrCreateByHubIdAsync(dto.HubIdentifier, ct);
            hubId = hub.Id;
        }

        if (hubId == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Either HubId or HubIdentifier must be provided"
            });
        }

        var createDto = dto with { HubId = hubId };
        var sensor = await _sensorService.CreateAsync(createDto, ct);
        return CreatedAtAction(nameof(GetById), new { id = sensor.Id }, sensor);
    }

    /// <summary>
    /// Registers or updates a Sensor (auto-registration)
    /// </summary>
    /// <param name="dto">Sensor data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The registered/updated Sensor</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(SensorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateSensorDto dto, CancellationToken ct)
    {
        // If HubId is provided as string, look it up
        var hubId = dto.HubId;
        if (hubId == null && !string.IsNullOrWhiteSpace(dto.HubIdentifier))
        {
            var hub = await _hubService.GetOrCreateByHubIdAsync(dto.HubIdentifier, ct);
            hubId = hub.Id;
        }

        if (hubId == null)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Request",
                Detail = "Either HubId or HubIdentifier must be provided"
            });
        }

        var sensor = await _sensorService.GetOrCreateBySensorIdAsync(hubId.Value, dto.SensorId, ct);

        // Notify clients
        await _hubContext.Clients.Group($"hub:{hubId}")
            .SendAsync("SensorRegistered", sensor, ct);

        return Ok(sensor);
    }

    /// <summary>
    /// Updates a Sensor
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Sensor</returns>
    /// <response code="200">Sensor successfully updated</response>
    /// <response code="404">Sensor not found</response>
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
    /// Updates the status of a Sensor
    /// </summary>
    /// <param name="id">Sensor-ID</param>
    /// <param name="dto">Status data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Status updated</response>
    /// <response code="404">Sensor not found</response>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] SensorStatusDto dto, CancellationToken ct)
    {
        var sensor = await _sensorService.GetByIdAsync(id, ct);
        if (sensor == null)
            return NotFound();

        await _sensorService.UpdateStatusAsync(id, dto, ct);
        return NoContent();
    }
}
