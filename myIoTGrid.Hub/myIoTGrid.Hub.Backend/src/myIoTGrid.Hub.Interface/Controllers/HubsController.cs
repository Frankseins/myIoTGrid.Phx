using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for Hubs (Raspberry Pi Gateways)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HubsController : ControllerBase
{
    private readonly IHubService _hubService;
    private readonly ISensorService _sensorService;

    public HubsController(IHubService hubService, ISensorService sensorService)
    {
        _hubService = hubService;
        _sensorService = sensorService;
    }

    /// <summary>
    /// Returns all registered Hubs
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of all Hubs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<HubDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var hubs = await _hubService.GetAllAsync(ct);
        return Ok(hubs);
    }

    /// <summary>
    /// Returns a Hub by ID
    /// </summary>
    /// <param name="id">Hub-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Hub</returns>
    /// <response code="200">Hub found</response>
    /// <response code="404">Hub not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var hub = await _hubService.GetByIdAsync(id, ct);

        if (hub == null)
            return NotFound();

        return Ok(hub);
    }

    /// <summary>
    /// Returns all Sensors for a Hub
    /// </summary>
    /// <param name="id">Hub-ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Sensors</returns>
    [HttpGet("{id:guid}/sensors")]
    [ProducesResponseType(typeof(IEnumerable<SensorDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSensors(Guid id, CancellationToken ct)
    {
        var sensors = await _sensorService.GetByHubAsync(id, ct);
        return Ok(sensors);
    }

    /// <summary>
    /// Creates a new Hub
    /// </summary>
    /// <param name="dto">Hub data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created Hub</returns>
    /// <response code="201">Hub successfully created</response>
    /// <response code="400">Invalid data or HubId already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(HubDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateHubDto dto, CancellationToken ct)
    {
        var hub = await _hubService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = hub.Id }, hub);
    }

    /// <summary>
    /// Updates a Hub
    /// </summary>
    /// <param name="id">Hub-ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Hub</returns>
    /// <response code="200">Hub successfully updated</response>
    /// <response code="404">Hub not found</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(HubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHubDto dto, CancellationToken ct)
    {
        var hub = await _hubService.UpdateAsync(id, dto, ct);

        if (hub == null)
            return NotFound();

        return Ok(hub);
    }
}
