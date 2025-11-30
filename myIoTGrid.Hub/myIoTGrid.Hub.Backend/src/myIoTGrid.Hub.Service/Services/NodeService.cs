using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using myIoTGrid.Hub.Domain.Entities;
using myIoTGrid.Hub.Domain.Enums;
using myIoTGrid.Hub.Domain.Interfaces;
using myIoTGrid.Hub.Infrastructure.Data;
using myIoTGrid.Hub.Service.Extensions;
using myIoTGrid.Hub.Service.Interfaces;
using myIoTGrid.Hub.Shared.DTOs;
using myIoTGrid.Hub.Shared.DTOs.Common;
using myIoTGrid.Hub.Shared.Extensions;

namespace myIoTGrid.Hub.Service.Services;

/// <summary>
/// Service for Node (ESP32/LoRa32 Device) management.
/// Matter-konform: Entspricht einem Matter Node.
/// </summary>
public class NodeService : INodeService
{
    private readonly HubDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly ILogger<NodeService> _logger;

    public NodeService(
        HubDbContext context,
        IUnitOfWork unitOfWork,
        ISignalRNotificationService signalRNotificationService,
        ILogger<NodeService> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _signalRNotificationService = signalRNotificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeDto>> GetAllAsync(CancellationToken ct = default)
    {
        var nodes = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

        return nodes.Select(n => n.ToDto());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<NodeDto>> GetByHubAsync(Guid hubId, CancellationToken ct = default)
    {
        var nodes = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .Where(n => n.HubId == hubId)
            .OrderBy(n => n.Name)
            .ToListAsync(ct);

        return nodes.Select(n => n.ToDto());
    }

    /// <inheritdoc />
    public async Task<PagedResultDto<NodeDto>> GetPagedAsync(QueryParamsDto queryParams, CancellationToken ct = default)
    {
        var query = _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .Include(n => n.Hub)
            .AsQueryable();

        // Global search (Name, NodeId)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            query = query.ApplySearch(
                queryParams.Search,
                n => n.Name,
                n => n.NodeId);
        }

        // Filter by HubId
        if (queryParams.Filters?.TryGetValue("hubId", out var hubIdFilter) == true &&
            Guid.TryParse(hubIdFilter, out var hubId))
        {
            query = query.Where(n => n.HubId == hubId);
        }

        // Filter by Protocol
        if (queryParams.Filters?.TryGetValue("protocol", out var protocolFilter) == true &&
            Enum.TryParse<Protocol>(protocolFilter, true, out var protocol))
        {
            query = query.Where(n => n.Protocol == protocol);
        }

        // Filter by IsOnline
        if (queryParams.Filters?.TryGetValue("isOnline", out var isOnlineFilter) == true &&
            bool.TryParse(isOnlineFilter, out var isOnline))
        {
            query = query.Where(n => n.IsOnline == isOnline);
        }

        // Date filter on LastSeen
        query = query.ApplyDateFilter(queryParams, n => n.LastSeen);

        // Total count before paging
        var totalRecords = await query.CountAsync(ct);

        // Apply sorting (default: Name ascending)
        query = query.ApplySort(queryParams, "Name");

        // Apply paging
        query = query.ApplyPaging(queryParams);

        var items = await query.ToListAsync(ct);

        return PagedResultDto<NodeDto>.Create(
            items.Select(n => n.ToDto()),
            totalRecords,
            queryParams);
    }

    /// <inheritdoc />
    public async Task<NodeDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        return node?.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto?> GetByNodeIdAsync(Guid hubId, string nodeId, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .AsNoTracking()
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == nodeId, ct);

        return node?.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto> GetOrCreateByNodeIdAsync(Guid hubId, string nodeId, CancellationToken ct = default)
    {
        var existingNode = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == nodeId, ct);

        if (existingNode != null)
        {
            // Update LastSeen and Online status
            existingNode.LastSeen = DateTime.UtcNow;
            existingNode.IsOnline = true;
            await _unitOfWork.SaveChangesAsync(ct);

            return existingNode.ToDto();
        }

        // Create new Node (Auto-Registration)
        var newNode = new Node
        {
            Id = Guid.NewGuid(),
            HubId = hubId,
            NodeId = nodeId,
            Name = GenerateNameFromNodeId(nodeId),
            Protocol = Protocol.WLAN,
            IsOnline = true,
            LastSeen = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Nodes.Add(newNode);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node auto-registered: {NodeId} ({Name}) for Hub {HubId}",
            newNode.NodeId, newNode.Name, hubId);

        return newNode.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto> CreateAsync(CreateNodeDto dto, CancellationToken ct = default)
    {
        var hubId = dto.HubId ?? throw new InvalidOperationException("HubId is required");

        // Check if NodeId already exists within the Hub
        var exists = await _context.Nodes
            .AsNoTracking()
            .AnyAsync(n => n.HubId == hubId && n.NodeId == dto.NodeId, ct);

        if (exists)
        {
            throw new InvalidOperationException($"Node with NodeId '{dto.NodeId}' already exists for this Hub.");
        }

        var node = dto.ToEntity(hubId);

        _context.Nodes.Add(node);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node created: {NodeId} ({Name})", node.NodeId, node.Name);

        return node.ToDto();
    }

    /// <inheritdoc />
    public async Task<NodeDto?> UpdateAsync(Guid id, UpdateNodeDto dto, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
        {
            _logger.LogWarning("Node not found: {NodeId}", id);
            return null;
        }

        node.ApplyUpdate(dto);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node updated: {NodeId}", id);

        return node.ToDto();
    }

    /// <inheritdoc />
    public async Task UpdateLastSeenAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return;

        node.LastSeen = DateTime.UtcNow;
        node.IsOnline = true;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SetOnlineStatusAsync(Guid id, bool isOnline, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .Include(n => n.Hub)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return;

        node.IsOnline = isOnline;

        if (!isOnline)
        {
            _logger.LogWarning("Node offline: {NodeId} ({Name})", node.NodeId, node.Name);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid id, NodeStatusDto status, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return;

        node.ApplyStatus(status);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<NodeDto> RegisterOrUpdateAsync(CreateNodeDto dto, CancellationToken ct = default)
    {
        var hubId = dto.HubId ?? throw new InvalidOperationException("HubId is required");

        var existingNode = await _context.Nodes
            .Include(n => n.SensorAssignments)
            .FirstOrDefaultAsync(n => n.HubId == hubId && n.NodeId == dto.NodeId, ct);

        if (existingNode != null)
        {
            // Update existing node
            existingNode.LastSeen = DateTime.UtcNow;
            existingNode.IsOnline = true;

            if (!string.IsNullOrEmpty(dto.Name))
                existingNode.Name = dto.Name;

            if (dto.Location != null)
                existingNode.Location = dto.Location.ToEntity();

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogDebug("Node updated via registration: {NodeId}", dto.NodeId);
            return existingNode.ToDto();
        }

        // Create new Node
        return await CreateAsync(dto, ct);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _context.Nodes
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null) return false;

        _context.Nodes.Remove(node);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Node deleted: {NodeId} ({Name})", node.NodeId, node.Name);
        return true;
    }

    /// <summary>
    /// Generates a name from the NodeId
    /// </summary>
    private static string GenerateNameFromNodeId(string nodeId)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) return "Unknown Node";

        // "wetterstation-garten-01" -> "Wetterstation Garten 01"
        var parts = nodeId.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var formattedParts = parts.Select(s =>
            s.Length > 0 ? char.ToUpper(s[0]) + s[1..].ToLower() : s);

        return string.Join(" ", formattedParts);
    }
}
