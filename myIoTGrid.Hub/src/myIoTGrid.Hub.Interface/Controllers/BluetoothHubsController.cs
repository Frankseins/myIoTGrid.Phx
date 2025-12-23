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
    private readonly IBluetoothPairingService _pairingService;
    private readonly IBleGattClientService _gattClientService;

    public BluetoothHubsController(
        IBluetoothHubService bluetoothHubService,
        IBluetoothPairingService pairingService,
        IBleGattClientService gattClientService)
    {
        _bluetoothHubService = bluetoothHubService;
        _pairingService = pairingService;
        _gattClientService = gattClientService;
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

    // =========================================================================
    // Backend BLE Pairing Endpoints (Sprint BT-02)
    // These endpoints are used when pairing in Bluetooth mode
    // =========================================================================

    /// <summary>
    /// Scans for BLE devices with myIoTGrid prefix.
    /// This runs bluetoothctl scan on the Raspberry Pi.
    /// </summary>
    /// <param name="timeoutSeconds">Scan timeout in seconds (default: 10)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of discovered myIoTGrid BLE devices</returns>
    /// <response code="200">Scan complete</response>
    [HttpGet("scan")]
    [ProducesResponseType(typeof(IEnumerable<ScannedBleDeviceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanForDevices([FromQuery] int timeoutSeconds = 10, CancellationToken ct = default)
    {
        var devices = await _pairingService.ScanForDevicesAsync(timeoutSeconds, ct);
        return Ok(devices);
    }

    /// <summary>
    /// Pairs with a BLE device by MAC address.
    /// This runs bluetoothctl pair/trust on the Raspberry Pi.
    /// Used for Bluetooth sensor mode where pairing happens on the backend.
    /// </summary>
    /// <param name="dto">Pairing request with MAC address</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Pairing result</returns>
    /// <response code="200">Pairing successful</response>
    /// <response code="400">Pairing failed</response>
    [HttpPost("pair")]
    [ProducesResponseType(typeof(BlePairingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlePairingResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PairDevice([FromBody] PairBleDeviceRequestDto dto, CancellationToken ct)
    {
        var result = await _pairingService.PairDeviceAsync(dto.MacAddress, ct);

        if (!result.Success)
            return BadRequest(result);

        // If NodeId was provided, register the device with the BluetoothHub
        if (!string.IsNullOrEmpty(dto.NodeId))
        {
            var registerDto = new RegisterBleDeviceDto(
                dto.NodeId,
                result.DeviceName ?? dto.MacAddress,
                dto.MacAddress
            );
            await _bluetoothHubService.RegisterBleDeviceFromFrontendAsync(registerDto, ct);
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets the list of paired BLE devices on the Raspberry Pi.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>List of paired devices</returns>
    /// <response code="200">Paired devices retrieved</response>
    [HttpGet("paired-devices")]
    [ProducesResponseType(typeof(IEnumerable<ScannedBleDeviceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPairedDevices(CancellationToken ct)
    {
        var devices = await _pairingService.GetPairedDevicesAsync(ct);
        return Ok(devices);
    }

    /// <summary>
    /// Removes pairing for a BLE device.
    /// </summary>
    /// <param name="macAddress">MAC address of the device to unpair</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    /// <response code="204">Device unpaired</response>
    /// <response code="400">Unpairing failed</response>
    [HttpDelete("pair/{macAddress}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnpairDevice(string macAddress, CancellationToken ct)
    {
        var success = await _pairingService.UnpairDeviceAsync(macAddress, ct);

        if (!success)
            return BadRequest(new { error = "Failed to unpair device" });

        return NoContent();
    }

    /// <summary>
    /// Scans for devices and pairs with a specific device in one call.
    /// Convenience endpoint for the frontend wizard.
    /// </summary>
    /// <param name="dto">Pairing request with MAC address and optional NodeId</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Pairing result with registration info</returns>
    /// <response code="200">Scan and pair successful</response>
    /// <response code="400">Failed</response>
    [HttpPost("scan-and-pair")]
    [ProducesResponseType(typeof(BlePairingResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BlePairingResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanAndPair([FromBody] PairBleDeviceRequestDto dto, CancellationToken ct)
    {
        // First scan to make sure device is discoverable
        await _pairingService.ScanForDevicesAsync(5, ct);

        // Then pair
        var result = await _pairingService.PairDeviceAsync(dto.MacAddress, ct);

        if (!result.Success)
            return BadRequest(result);

        // Register with BluetoothHub if NodeId provided
        if (!string.IsNullOrEmpty(dto.NodeId))
        {
            var registerDto = new RegisterBleDeviceDto(
                dto.NodeId,
                result.DeviceName ?? dto.MacAddress,
                dto.MacAddress
            );
            await _bluetoothHubService.RegisterBleDeviceFromFrontendAsync(registerDto, ct);
        }

        return Ok(result);
    }

    // =========================================================================
    // BLE GATT Client Endpoints (Sprint BT-01)
    // For bidirectional communication with ESP32 sensors via GATT
    // =========================================================================

    /// <summary>
    /// Connects to an ESP32 sensor via BLE GATT and reads device info.
    /// </summary>
    /// <param name="macAddress">MAC address of the ESP32 (format: AA:BB:CC:DD:EE:FF)</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Connection result with device info</returns>
    /// <response code="200">Connected successfully</response>
    /// <response code="400">Connection failed</response>
    [HttpPost("gatt/connect/{macAddress}")]
    [ProducesResponseType(typeof(BleConnectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleConnectionResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattConnect(string macAddress, CancellationToken ct)
    {
        var result = await _gattClientService.ConnectAsync(macAddress, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Disconnects from the currently connected ESP32.
    /// </summary>
    /// <returns>No content</returns>
    /// <response code="204">Disconnected</response>
    [HttpPost("gatt/disconnect")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GattDisconnect()
    {
        await _gattClientService.DisconnectAsync();
        return NoContent();
    }

    /// <summary>
    /// Gets the current GATT connection status.
    /// </summary>
    /// <returns>Connection status</returns>
    /// <response code="200">Status returned</response>
    [HttpGet("gatt/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GattStatus()
    {
        return Ok(new
        {
            IsConnected = _gattClientService.IsConnected,
            ConnectedDeviceMac = _gattClientService.ConnectedDeviceMac,
            IsAuthenticated = _gattClientService.IsAuthenticated
        });
    }

    /// <summary>
    /// Authenticates with the connected ESP32 using a node ID.
    /// The node ID is hashed and compared with the device's hash.
    /// </summary>
    /// <param name="nodeId">Node ID to authenticate with</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Authentication result</returns>
    /// <response code="200">Authentication successful</response>
    /// <response code="400">Authentication failed</response>
    [HttpPost("gatt/authenticate")]
    [ProducesResponseType(typeof(BleAuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleAuthResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattAuthenticate([FromBody] string nodeId, CancellationToken ct)
    {
        var hash = _gattClientService.ComputeNodeIdHash(nodeId);
        var result = await _gattClientService.AuthenticateAsync(hash, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Reads current sensor data from the connected ESP32.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Current sensor readings</returns>
    /// <response code="200">Sensor data retrieved</response>
    /// <response code="400">Failed to read sensor data</response>
    [HttpGet("gatt/sensor-data")]
    [ProducesResponseType(typeof(BleSensorDataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattReadSensorData(CancellationToken ct)
    {
        var data = await _gattClientService.ReadSensorDataAsync(ct);

        if (data == null)
            return BadRequest(new { error = "Failed to read sensor data. Is device connected?" });

        return Ok(data);
    }

    /// <summary>
    /// Reads device info from the connected ESP32.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Device info</returns>
    /// <response code="200">Device info retrieved</response>
    /// <response code="400">Failed to read device info</response>
    [HttpGet("gatt/device-info")]
    [ProducesResponseType(typeof(BleDeviceInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattReadDeviceInfo(CancellationToken ct)
    {
        var info = await _gattClientService.ReadDeviceInfoAsync(ct);

        if (info == null)
            return BadRequest(new { error = "Failed to read device info. Is device connected?" });

        return Ok(info);
    }

    /// <summary>
    /// Sends WiFi credentials to the connected ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="config">WiFi configuration</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Config result</returns>
    /// <response code="200">WiFi config sent</response>
    /// <response code="400">Failed to send config</response>
    [HttpPost("gatt/config/wifi")]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattSetWifi([FromBody] BleWifiConfigDto config, CancellationToken ct)
    {
        var result = await _gattClientService.SetWifiAsync(config, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Sends Hub URL configuration to the connected ESP32.
    /// Requires authentication first.
    /// </summary>
    /// <param name="config">Hub URL configuration</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Config result</returns>
    /// <response code="200">Hub URL config sent</response>
    /// <response code="400">Failed to send config</response>
    [HttpPost("gatt/config/hub-url")]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattSetHubUrl([FromBody] BleHubUrlConfigDto config, CancellationToken ct)
    {
        var result = await _gattClientService.SetHubUrlAsync(config, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// Reboots the connected ESP32 to apply new settings.
    /// Requires authentication first.
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Reboot result</returns>
    /// <response code="200">Reboot command sent</response>
    /// <response code="400">Failed to send reboot</response>
    [HttpPost("gatt/reboot")]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattReboot(CancellationToken ct)
    {
        var result = await _gattClientService.RebootAsync(ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    /// <summary>
    /// High-level provisioning: Connects, authenticates, and configures an ESP32 in one call.
    /// </summary>
    /// <param name="request">Provisioning request with all configuration</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Provisioning result</returns>
    /// <response code="200">Provisioning successful</response>
    /// <response code="400">Provisioning failed</response>
    [HttpPost("gatt/provision")]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BleConfigResultDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GattProvision([FromBody] BleProvisioningRequestDto request, CancellationToken ct)
    {
        var result = await _gattClientService.ProvisionDeviceAsync(
            request.MacAddress,
            request.NodeId,
            request.WifiConfig,
            request.HubUrlConfig,
            request.IntervalSeconds,
            ct);

        if (!result.Success)
            return BadRequest(result);

        // If provisioning successful, register the device with BluetoothHub
        var registerDto = new RegisterBleDeviceDto(
            request.NodeId,
            $"myIoTGrid-{request.MacAddress[^5..].Replace(":", "")}",
            request.MacAddress
        );
        await _bluetoothHubService.RegisterBleDeviceFromFrontendAsync(registerDto, ct);

        return Ok(result);
    }
}
