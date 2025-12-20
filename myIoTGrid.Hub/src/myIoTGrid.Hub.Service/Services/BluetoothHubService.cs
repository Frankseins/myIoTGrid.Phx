using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for BluetoothHub (Bluetooth Gateway) management.
/// BluetoothHubs are Raspberry Pi devices that receive sensor data via BLE
/// from ESP32 devices and forward it to the main Hub API.
/// </summary>
public class BluetoothHubService : IBluetoothHubService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubService _hubService;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<BluetoothHubService> _logger;

    public BluetoothHubService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        IHubService hubService,
        ISignalRNotificationService signalRNotificationService,
        ILogger<BluetoothHubService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _hubService = hubService;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<BluetoothHubDto>> GetAllAsync(CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var bluetoothHubs = await _context.BluetoothHubs
            .AsNoTracking()
            .Include(b => b.Nodes)
            .Where(b => b.HubId == hub.Id)
            .OrderBy(b => b.Name)
            .ToListAsync(ct);

        return bluetoothHubs.ToDtos();
    }

    /// <inheritdoc />
    public async Task<BluetoothHubDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var bluetoothHub = await _context.BluetoothHubs
            .AsNoTracking()
            .Include(b => b.Nodes)
            .FirstOrDefaultAsync(b => b.Id == id && b.HubId == hub.Id, ct);

        return bluetoothHub?.ToDto();
    }

    /// <inheritdoc />
    public async Task<BluetoothHubDto?> GetByMacAddressAsync(string macAddress, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var bluetoothHub = await _context.BluetoothHubs
            .AsNoTracking()
            .Include(b => b.Nodes)
            .FirstOrDefaultAsync(b => b.MacAddress == macAddress && b.HubId == hub.Id, ct);

        return bluetoothHub?.ToDto();
    }

    /// <inheritdoc />
    public async Task<BluetoothHubDto> CreateAsync(CreateBluetoothHubDto dto, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        // Check if MAC address already exists
        if (!string.IsNullOrWhiteSpace(dto.MacAddress))
        {
            var existing = await _context.BluetoothHubs
                .FirstOrDefaultAsync(b => b.MacAddress == dto.MacAddress, ct);

            if (existing != null)
            {
                throw new InvalidOperationException($"BluetoothHub with MAC address {dto.MacAddress} already exists");
            }
        }

        var bluetoothHub = dto.ToEntity(hub.Id);
        _context.BluetoothHubs.Add(bluetoothHub);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("BluetoothHub created: {Name} ({Id})", bluetoothHub.Name, bluetoothHub.Id);

        return bluetoothHub.ToDto(0);
    }

    /// <inheritdoc />
    public async Task<BluetoothHubDto?> UpdateAsync(Guid id, UpdateBluetoothHubDto dto, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var bluetoothHub = await _context.BluetoothHubs
            .Include(b => b.Nodes)
            .FirstOrDefaultAsync(b => b.Id == id && b.HubId == hub.Id, ct);

        if (bluetoothHub == null)
        {
            _logger.LogWarning("BluetoothHub not found: {Id}", id);
            return null;
        }

        // Check if new MAC address conflicts with existing
        if (!string.IsNullOrWhiteSpace(dto.MacAddress) && dto.MacAddress != bluetoothHub.MacAddress)
        {
            var existing = await _context.BluetoothHubs
                .FirstOrDefaultAsync(b => b.MacAddress == dto.MacAddress && b.Id != id, ct);

            if (existing != null)
            {
                throw new InvalidOperationException($"BluetoothHub with MAC address {dto.MacAddress} already exists");
            }
        }

        bluetoothHub.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("BluetoothHub updated: {Name} ({Id})", bluetoothHub.Name, bluetoothHub.Id);

        return bluetoothHub.ToDto();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var bluetoothHub = await _context.BluetoothHubs
            .Include(b => b.Nodes)
            .FirstOrDefaultAsync(b => b.Id == id && b.HubId == hub.Id, ct);

        if (bluetoothHub == null)
        {
            _logger.LogWarning("BluetoothHub not found for deletion: {Id}", id);
            return false;
        }

        // Disassociate all nodes
        foreach (var node in bluetoothHub.Nodes)
        {
            node.BluetoothHubId = null;
            node.Protocol = Protocol.WLAN; // Reset to default protocol
        }

        _context.BluetoothHubs.Remove(bluetoothHub);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("BluetoothHub deleted: {Name} ({Id})", bluetoothHub.Name, bluetoothHub.Id);

        return true;
    }

    /// <inheritdoc />
    public async Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var bluetoothHub = await _context.BluetoothHubs
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (bluetoothHub == null) return;

        bluetoothHub.LastSeen = DateTime.UtcNow;
        bluetoothHub.Status = "Active";
        bluetoothHub.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var bluetoothHub = await _context.BluetoothHubs
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (bluetoothHub == null) return;

        var previousStatus = bluetoothHub.Status;
        bluetoothHub.Status = status;
        bluetoothHub.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        if (previousStatus != status)
        {
            _logger.LogInformation("BluetoothHub status changed: {Name} ({Id}) {Previous} -> {New}",
                bluetoothHub.Name, bluetoothHub.Id, previousStatus, status);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeDto>> GetNodesAsync(Guid bluetoothHubId, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var nodes = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
                .ThenInclude(a => a.Sensor)
            .Where(n => n.BluetoothHubId == bluetoothHubId && n.HubId == hub.Id)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

        return nodes.Select(n => n.ToDto());
    }

    /// <inheritdoc />
    public async Task<bool> AssociateNodeAsync(Guid bluetoothHubId, Guid nodeId, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var bluetoothHub = await _context.BluetoothHubs
            .FirstOrDefaultAsync(b => b.Id == bluetoothHubId && b.HubId == hub.Id, ct);

        if (bluetoothHub == null)
        {
            _logger.LogWarning("BluetoothHub not found: {Id}", bluetoothHubId);
            return false;
        }

        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.HubId == hub.Id, ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found: {Id}", nodeId);
            return false;
        }

        node.BluetoothHubId = bluetoothHubId;
        node.Protocol = Protocol.Bluetooth;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node {NodeId} associated with BluetoothHub {BluetoothHubId}",
            node.NodeId, bluetoothHub.Name);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DisassociateNodeAsync(Guid nodeId, CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == nodeId && n.HubId == hub.Id, ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found: {Id}", nodeId);
            return false;
        }

        var previousBluetoothHubId = node.BluetoothHubId;
        node.BluetoothHubId = null;
        node.Protocol = Protocol.WLAN;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node {NodeId} disassociated from BluetoothHub {BluetoothHubId}",
            node.NodeId, previousBluetoothHubId);

        return true;
    }

    /// <inheritdoc />
    public async Task<BleDeviceRegistrationResultDto> RegisterBleDeviceFromFrontendAsync(
        RegisterBleDeviceDto dto, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Registering BLE device from frontend: {NodeId} ({DeviceName})",
                dto.NodeId, dto.DeviceName);

            // Get or create the default BluetoothHub for this Hub
            var bluetoothHub = await GetOrCreateDefaultAsync(ct);

            // Find the node by NodeId (device ID string, e.g., "ESP32-AABBCCDDEE01")
            var hub = await _hubService.GetCurrentHubAsync(ct);
            var node = await _context.Nodes
                .FirstOrDefaultAsync(n => n.NodeId == dto.NodeId && n.HubId == hub.Id, ct);

            if (node == null)
            {
                _logger.LogWarning("Node not found for BLE registration: {NodeId}", dto.NodeId);
                return new BleDeviceRegistrationResultDto(
                    Success: false,
                    NodeId: null,
                    BluetoothHubId: bluetoothHub.Id,
                    Message: $"Node with ID '{dto.NodeId}' not found. Please create the node first.");
            }

            // Update node with BLE information
            node.BluetoothHubId = bluetoothHub.Id;
            node.Protocol = Protocol.Bluetooth;
            node.BleMacAddress = dto.MacAddress;
            node.BleDeviceName = dto.DeviceName;
            node.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("BLE device registered: Node {NodeId} -> BluetoothHub {BluetoothHubId}",
                node.NodeId, bluetoothHub.Id);

            return new BleDeviceRegistrationResultDto(
                Success: true,
                NodeId: node.Id,
                BluetoothHubId: bluetoothHub.Id,
                Message: "BLE device registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering BLE device: {NodeId}", dto.NodeId);
            return new BleDeviceRegistrationResultDto(
                Success: false,
                NodeId: null,
                BluetoothHubId: null,
                Message: $"Error registering BLE device: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<BluetoothHubDto> GetOrCreateDefaultAsync(CancellationToken ct = default)
    {
        var hub = await _hubService.GetCurrentHubAsync(ct);

        // Look for existing default BluetoothHub
        var bluetoothHub = await _context.BluetoothHubs
            .Include(b => b.Nodes)
            .FirstOrDefaultAsync(b => b.HubId == hub.Id && b.Name == "Hub BLE Gateway", ct);

        if (bluetoothHub != null)
        {
            return bluetoothHub.ToDto();
        }

        // Create default BluetoothHub for this Hub
        _logger.LogInformation("Creating default BluetoothHub for Hub {HubId}", hub.Id);

        bluetoothHub = new BluetoothHub
        {
            Id = Guid.NewGuid(),
            HubId = hub.Id,
            Name = "Hub BLE Gateway",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        _context.BluetoothHubs.Add(bluetoothHub);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Default BluetoothHub created: {Id}", bluetoothHub.Id);

        return bluetoothHub.ToDto(0);
    }
}
