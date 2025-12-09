using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Infrastructure.Data;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Node Hardware Status management (Sprint 8).
/// </summary>
public class NodeHardwareStatusService : INodeHardwareStatusService
{
    private readonly HubDbContext _context;
    private readonly ILogger<NodeHardwareStatusService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public NodeHardwareStatusService(HubDbContext context, ILogger<NodeHardwareStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NodeHardwareStatusDto?> ReportHardwareStatusAsync(ReportHardwareStatusDto dto, CancellationToken ct = default)
    {
        // Search by both MacAddress and NodeId (ESP32 may send either as serialNumber)
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.MacAddress == dto.SerialNumber || n.NodeId == dto.SerialNumber, ct);

        if (node == null)
        {
            _logger.LogWarning("Hardware status report from unknown node: {SerialNumber}", dto.SerialNumber);
            return null;
        }

        // Update node fields
        node.FirmwareVersion = dto.FirmwareVersion;
        node.HardwareStatusReportedAt = DateTime.UtcNow;

        // Create the internal status object to store
        var internalStatus = new
        {
            dto.DetectedDevices,
            dto.Storage,
            dto.BusStatus
        };
        node.HardwareStatusJson = JsonSerializer.Serialize(internalStatus, JsonOptions);

        // Update sync info from storage status
        node.PendingSyncCount = dto.Storage.PendingSyncCount;
        if (dto.Storage.LastSyncAt.HasValue)
        {
            node.LastSyncAt = dto.Storage.LastSyncAt;
        }
        node.LastSyncError = dto.Storage.LastSyncError;

        // Parse storage mode
        if (Enum.TryParse<StorageMode>(dto.Storage.Mode, true, out var storageMode))
        {
            node.StorageMode = storageMode;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Hardware status reported for node {SerialNumber}: {DeviceCount} devices detected",
            dto.SerialNumber, dto.DetectedDevices.Count);

        return await GetHardwareStatusAsync(node.Id, ct);
    }

    public async Task<NodeHardwareStatusDto?> GetHardwareStatusAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .ThenInclude(a => a.Sensor)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == nodeId, ct);

        if (node == null)
        {
            return null;
        }

        return BuildHardwareStatusDto(node);
    }

    public async Task<NodeHardwareStatusDto?> GetHardwareStatusBySerialAsync(string serialNumber, CancellationToken ct = default)
    {
        // Search by both MacAddress and NodeId
        var node = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .ThenInclude(a => a.Sensor)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.MacAddress == serialNumber || n.NodeId == serialNumber, ct);

        if (node == null)
        {
            return null;
        }

        return BuildHardwareStatusDto(node);
    }

    private NodeHardwareStatusDto BuildHardwareStatusDto(Node node)
    {
        // Parse stored JSON or use defaults
        List<DetectedDeviceDto> detectedDevices = new();
        StorageStatusDto storage = new(false, "REMOTE_ONLY", 0, 0, 0, 0, null, null);
        BusStatusDto busStatus = new(false, 0, new List<string>(), false, 0, false, false);

        if (!string.IsNullOrEmpty(node.HardwareStatusJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(node.HardwareStatusJson);
                var root = doc.RootElement;

                // Parse detected devices
                if (root.TryGetProperty("detectedDevices", out var devicesElement))
                {
                    detectedDevices = JsonSerializer.Deserialize<List<DetectedDeviceDto>>(
                        devicesElement.GetRawText(), JsonOptions) ?? new();
                }

                // Parse storage
                if (root.TryGetProperty("storage", out var storageElement))
                {
                    storage = JsonSerializer.Deserialize<StorageStatusDto>(
                        storageElement.GetRawText(), JsonOptions) ?? storage;
                }

                // Parse bus status
                if (root.TryGetProperty("busStatus", out var busElement))
                {
                    busStatus = JsonSerializer.Deserialize<BusStatusDto>(
                        busElement.GetRawText(), JsonOptions) ?? busStatus;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse hardware status JSON for node {NodeId}", node.Id);
            }
        }

        // Calculate summary
        var sensorsConfigured = node.SensorAssignments.Count;
        var sensorsOk = detectedDevices.Count(d => d.Status == "OK" && d.SensorCode != null);
        var sensorsError = detectedDevices.Count(d => d.Status == "Error");
        var hasGps = detectedDevices.Any(d => d.DeviceType.Contains("GPS", StringComparison.OrdinalIgnoreCase));

        var overallStatus = sensorsError > 0 ? "Error" :
                           (detectedDevices.Count < sensorsConfigured) ? "Warning" : "OK";

        var summary = new HardwareSummaryDto(
            TotalDevicesDetected: detectedDevices.Count,
            SensorsConfigured: sensorsConfigured,
            SensorsOk: sensorsOk,
            SensorsError: sensorsError,
            HasSdCard: storage.Available,
            HasGps: hasGps,
            OverallStatus: overallStatus
        );

        return new NodeHardwareStatusDto(
            NodeId: node.Id,
            SerialNumber: node.MacAddress,
            FirmwareVersion: node.FirmwareVersion ?? "Unknown",
            HardwareType: "ESP32",
            ReportedAt: node.HardwareStatusReportedAt ?? node.CreatedAt,
            Summary: summary,
            DetectedDevices: detectedDevices,
            Storage: storage,
            BusStatus: busStatus
        );
    }
}
