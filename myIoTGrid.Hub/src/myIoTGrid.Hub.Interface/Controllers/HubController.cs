using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace myIoTGrid.Hub.Interface.Controllers;

/// <summary>
/// REST API Controller for the Hub (Single-Hub-Architecture).
/// Only one Hub per installation - no create/delete, only read/update.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HubController : ControllerBase
{
    private readonly IHubService _hubService;
    private readonly INodeService _nodeService;

    public HubController(IHubService hubService, INodeService nodeService)
    {
        _hubService = hubService;
        _nodeService = nodeService;
    }

    /// <summary>
    /// Returns the current Hub
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The Hub</returns>
    /// <response code="200">Hub found</response>
    [HttpGet]
    [ProducesResponseType(typeof(HubDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);
        return Ok(hub);
    }

    /// <summary>
    /// Updates the current Hub
    /// </summary>
    /// <param name="dto">Update data</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The updated Hub</returns>
    /// <response code="200">Hub successfully updated</response>
    [HttpPut]
    [ProducesResponseType(typeof(HubDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdateHubDto dto, CancellationToken ct)
    {
        var hub = await _hubService.UpdateCurrentHubAsync(dto, ct);
        return Ok(hub);
    }

    /// <summary>
    /// Returns the Hub status
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Hub status information</returns>
    /// <response code="200">Status retrieved</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(HubStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var status = await _hubService.GetStatusAsync(ct);
        return Ok(status);
    }

    /// <summary>
    /// Returns all Nodes for this Hub
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of Nodes</returns>
    [HttpGet("nodes")]
    [ProducesResponseType(typeof(IEnumerable<NodeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNodes(CancellationToken ct)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);
        var nodes = await _nodeService.GetByHubAsync(hub.Id, ct);
        return Ok(nodes);
    }

    /// <summary>
    /// Returns the provisioning settings for BLE setup wizard
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Provisioning settings (WiFi, API URL)</returns>
    /// <response code="200">Settings retrieved</response>
    [HttpGet("provisioning-settings")]
    [ProducesResponseType(typeof(HubProvisioningSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvisioningSettings(CancellationToken ct)
    {
        var settings = await _hubService.GetProvisioningSettingsAsync(ct);
        return Ok(settings);
    }
}
