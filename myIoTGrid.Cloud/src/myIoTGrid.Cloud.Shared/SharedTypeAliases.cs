// =============================================================================
// myIoTGrid.Cloud.Shared - Type Aliases
// =============================================================================
// Re-exports from myIoTGrid.Shared.Common for backward compatibility
// This allows Cloud-specific projects to reference types via Cloud.Shared
// =============================================================================

global using SharedLocationDto = myIoTGrid.Shared.Common.DTOs.LocationDto;
global using SharedTenantDto = myIoTGrid.Shared.Common.DTOs.TenantDto;
global using SharedAlertLevelDto = myIoTGrid.Shared.Common.Enums.AlertLevelDto;

// Entities
global using Tenant = myIoTGrid.Shared.Common.Entities.Tenant;
global using Hub = myIoTGrid.Shared.Common.Entities.Hub;
global using Node = myIoTGrid.Shared.Common.Entities.Node;
global using Sensor = myIoTGrid.Shared.Common.Entities.Sensor;
global using SensorCapability = myIoTGrid.Shared.Common.Entities.SensorCapability;
global using NodeSensorAssignment = myIoTGrid.Shared.Common.Entities.NodeSensorAssignment;
global using Reading = myIoTGrid.Shared.Common.Entities.Reading;
global using Alert = myIoTGrid.Shared.Common.Entities.Alert;
global using AlertType = myIoTGrid.Shared.Common.Entities.AlertType;
global using SyncedNode = myIoTGrid.Shared.Common.Entities.SyncedNode;
global using SyncedReading = myIoTGrid.Shared.Common.Entities.SyncedReading;
global using NodeDebugLog = myIoTGrid.Shared.Common.Entities.NodeDebugLog;

// Enums
global using AlertLevel = myIoTGrid.Shared.Common.Enums.AlertLevel;
global using AlertSource = myIoTGrid.Shared.Common.Enums.AlertSource;
global using Protocol = myIoTGrid.Shared.Common.Enums.Protocol;
global using NodeStatus = myIoTGrid.Shared.Common.Enums.NodeStatus;
global using DebugLevel = myIoTGrid.Shared.Common.Enums.DebugLevel;

// Value Objects
global using Location = myIoTGrid.Shared.Common.ValueObjects.Location;

// DTOs
global using TenantDto = myIoTGrid.Shared.Common.DTOs.TenantDto;
global using HubDto = myIoTGrid.Shared.Common.DTOs.HubDto;
global using NodeDto = myIoTGrid.Shared.Common.DTOs.NodeDto;
global using SensorDto = myIoTGrid.Shared.Common.DTOs.SensorDto;
global using SensorCapabilityDto = myIoTGrid.Shared.Common.DTOs.SensorCapabilityDto;
global using NodeSensorAssignmentDto = myIoTGrid.Shared.Common.DTOs.NodeSensorAssignmentDto;
global using ReadingDto = myIoTGrid.Shared.Common.DTOs.ReadingDto;
global using AlertDto = myIoTGrid.Shared.Common.DTOs.AlertDto;
global using AlertTypeDto = myIoTGrid.Shared.Common.DTOs.AlertTypeDto;
global using LocationDto = myIoTGrid.Shared.Common.DTOs.LocationDto;
global using NodeDebugLogDto = myIoTGrid.Shared.Common.DTOs.NodeDebugLogDto;
global using PaginatedResultDto = myIoTGrid.Shared.Common.DTOs.PaginatedResultDto<object>;

// Create DTOs
global using CreateTenantDto = myIoTGrid.Shared.Common.DTOs.CreateTenantDto;
global using CreateHubDto = myIoTGrid.Shared.Common.DTOs.CreateHubDto;
global using CreateNodeDto = myIoTGrid.Shared.Common.DTOs.CreateNodeDto;
global using CreateSensorDto = myIoTGrid.Shared.Common.DTOs.CreateSensorDto;
global using CreateSensorCapabilityDto = myIoTGrid.Shared.Common.DTOs.CreateSensorCapabilityDto;
global using CreateNodeSensorAssignmentDto = myIoTGrid.Shared.Common.DTOs.CreateNodeSensorAssignmentDto;
global using CreateReadingDto = myIoTGrid.Shared.Common.DTOs.CreateReadingDto;
global using CreateAlertDto = myIoTGrid.Shared.Common.DTOs.CreateAlertDto;
global using CreateAlertTypeDto = myIoTGrid.Shared.Common.DTOs.CreateAlertTypeDto;

// Update DTOs
global using UpdateTenantDto = myIoTGrid.Shared.Common.DTOs.UpdateTenantDto;
global using UpdateHubDto = myIoTGrid.Shared.Common.DTOs.UpdateHubDto;
global using UpdateNodeDto = myIoTGrid.Shared.Common.DTOs.UpdateNodeDto;
global using UpdateSensorDto = myIoTGrid.Shared.Common.DTOs.UpdateSensorDto;
global using UpdateNodeSensorAssignmentDto = myIoTGrid.Shared.Common.DTOs.UpdateNodeSensorAssignmentDto;
global using UpdateAlertDto = myIoTGrid.Shared.Common.DTOs.UpdateAlertDto;

// Interfaces
global using ITenantEntity = myIoTGrid.Shared.Common.Interfaces.ITenantEntity;
global using IEntity = myIoTGrid.Shared.Common.Interfaces.IEntity;

namespace myIoTGrid.Cloud.Shared;

/// <summary>
/// Marker class for the Cloud.Shared assembly
/// </summary>
public static class CloudSharedMarker
{
    public const string AssemblyName = "myIoTGrid.Cloud.Shared";
}
