using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using myIoTGrid.Shared.Common.DTOs;
using myIoTGrid.Shared.Contracts.Services;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for BluetoothHub (Bluetooth Gateway) management.
/// BluetoothHubs are Raspberry Pi devices that receive sensor data via BLE
/// from ESP32 devices and forward it to the main Hub API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BluetoothHubsController : ControllerBase
{
    private readonly IBluetoothHubService _bluetoothHubService;

    public BluetoothHubsController(IBluetoothHubService bluetoothHubService)
    {
        _bluetoothHubService = bluetoothHubService;
    }

    /// <summary>
    /// Returns all BluetoothHubs for the current Hub
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of BluetoothHubs</returns>
    /// <response code="200">BluetoothHubs retrieved</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BluetoothHubDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var bluetoothHubs = await _bluetoothHubService.GetAllAsync(ct);
        return Ok(bluetoothHubs);
    }

    /// <summary>
    /// Returns a BluetoothHub by ID
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The BluetoothHub</returns>
    /// <response code="200">BluetoothHub found</response>
    /// <response code="404">BluetoothHub not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BluetoothHubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var bluetoothHub = await _bluetoothHubService.GetByIdAsync(id, ct);

        if (bluetoothHub == null)
            return NotFound();

        return Ok(bluetoothHub);
    }

    /// <summary>
    /// Returns a BluetoothHub by MAC address
    /// </summary>
    /// <param name="macAddress">MAC address (format: AA:BB:CC:DD:EE:FF)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The BluetoothHub</returns>
    /// <response code="200">BluetoothHub found</response>
    /// <response code="404">BluetoothHub not found</response>
    [HttpGet("by-mac/{macAddress}")]
    [ProducesResponseType(typeof(BluetoothHubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByMacAddress(string macAddress, CancellationToken ct)
    {
        var bluetoothHub = await _bluetoothHubService.GetByMacAddressAsync(macAddress, ct);

        if (bluetoothHub == null)
            return NotFound();

        return Ok(bluetoothHub);
    }

    /// <summary>
    /// Creates a new BluetoothHub
    /// </summary>
    /// <param name="dto">BluetoothHub creation data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The created BluetoothHub</returns>
    /// <response code="201">BluetoothHub created</response>
    /// <response code="400">Invalid data</response>
    /// <response code="409">MAC address already exists</response>
    [HttpPost]
    [ProducesResponseType(typeof(BluetoothHubDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBluetoothHubDto dto, CancellationToken ct)
    {
        try
        {
            var bluetoothHub = await _bluetoothHubService.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = bluetoothHub.Id }, bluetoothHub);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing BluetoothHub
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated BluetoothHub</returns>
    /// <response code="200">BluetoothHub updated</response>
    /// <response code="404">BluetoothHub not found</response>
    /// <response code="409">MAC address already exists</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BluetoothHubDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBluetoothHubDto dto, CancellationToken ct)
    {
        try
        {
            var bluetoothHub = await _bluetoothHubService.UpdateAsync(id, dto, ct);

            if (bluetoothHub == null)
                return NotFound();

            return Ok(bluetoothHub);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a BluetoothHub
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">BluetoothHub deleted</response>
    /// <response code="404">BluetoothHub not found</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _bluetoothHubService.DeleteAsync(id, ct);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Updates the LastSeen timestamp (heartbeat from BluetoothHub worker)
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Heartbeat recorded</response>
    [HttpPost("{id:guid}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Heartbeat(Guid id, CancellationToken ct)
    {
        await _bluetoothHubService.UpdateLastSeenAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Sets the status of a BluetoothHub (Active, Inactive, Error)
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="status">New status</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Status updated</response>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] string status, CancellationToken ct)
    {
        await _bluetoothHubService.SetStatusAsync(id, status, ct);
        return NoContent();
    }

    /// <summary>
    /// Returns all Nodes connected via a specific BluetoothHub
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Nodes</returns>
    /// <response code="200">Nodes retrieved</response>
    [HttpGet("{id:guid}/nodes")]
    [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNodes(Guid id, CancellationToken ct)
    {
        var nodes = await _bluetoothHubService.GetNodesAsync(id, ct);
        return Ok(nodes);
    }

    /// <summary>
    /// Associates a Node with a BluetoothHub
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="nodeId">Node ID to associate</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Node associated</response>
    /// <response code="404">BluetoothHub or Node not found</response>
    [HttpPost("{id:guid}/nodes/{nodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssociateNode(Guid id, Guid nodeId, CancellationToken ct)
    {
        var associated = await _bluetoothHubService.AssociateNodeAsync(id, nodeId, ct);

        if (!associated)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Removes the BluetoothHub association from a Node
    /// </summary>
    /// <param name="id">BluetoothHub ID</param>
    /// <param name="nodeId">Node ID to disassociate</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Node disassociated</response>
    /// <response code="404">Node not found</response>
    [HttpDelete("{id:guid}/nodes/{nodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisassociateNode(Guid id, Guid nodeId, CancellationToken ct)
    {
        var disassociated = await _bluetoothHubService.DisassociateNodeAsync(nodeId, ct);

        if (!disassociated)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Registers a BLE device paired via frontend (Web Bluetooth).
    /// This creates or gets the default BluetoothHub and associates the Node with it.
    /// </summary>
    /// <param name="dto">BLE device registration data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Registration result</returns>
    /// <response code="200">Registration successful</response>
    /// <response code="400">Invalid data or node not found</response>
    [HttpPost("register-device")]
    [ProducesResponseType(typeof(BleDeviceRegistrationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleDeviceRegistrationResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterBleDevice([FromBody] RegisterBleDeviceDto dto, CancellationToken ct)
    {
        var result = await _bluetoothHubService.RegisterBleDeviceFromFrontendAsync(dto, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Gets or creates the default BluetoothHub for this Hub.
    /// Useful for frontend to get the BluetoothHub ID before registering devices.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The default BluetoothHub</returns>
    /// <response code="200">BluetoothHub returned</response>
    [HttpGet("default")]
    [ProducesResponseType(typeof(BluetoothHubDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrCreateDefault(CancellationToken ct)
    {
        var bluetoothHub = await _bluetoothHubService.GetOrCreateDefaultAsync(ct);
        return Ok(bluetoothHub);
    }
}
