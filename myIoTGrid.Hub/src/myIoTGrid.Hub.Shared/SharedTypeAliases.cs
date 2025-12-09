// Re-export shared types from myIoTGrid.Shared.Common
// This allows existing Hub code to continue using myIoTGrid.Hub.Shared namespace
// while the actual types are defined in the shared library.

// DTOs - Re-export from Shared.Common
global using SharedLocationDto = myIoTGrid.Shared.Common.DTOs.LocationDto;
global using SharedTenantDto = myIoTGrid.Shared.Common.DTOs.TenantDto;

// Enums - Re-export from Shared.Common
global using SharedAlertLevelDto = myIoTGrid.Shared.Common.Enums.AlertLevelDto;
global using SharedAlertSourceDto = myIoTGrid.Shared.Common.Enums.AlertSourceDto;
global using SharedProtocolDto = myIoTGrid.Shared.Common.Enums.ProtocolDto;
global using SharedCommunicationProtocolDto = myIoTGrid.Shared.Common.Enums.CommunicationProtocolDto;
global using SharedStorageModeDto = myIoTGrid.Shared.Common.Enums.StorageModeDto;
global using SharedDebugLevelDto = myIoTGrid.Shared.Common.Enums.DebugLevelDto;
global using SharedLogCategoryDto = myIoTGrid.Shared.Common.Enums.LogCategoryDto;

// Constants
global using SharedSpecialTenants = myIoTGrid.Shared.Common.Constants.SpecialTenants;
global using SharedSensorTypes = myIoTGrid.Shared.Common.Constants.SensorTypes;

// Extensions
global using myIoTGrid.Shared.Utilities.Extensions;

// Converters - Re-export from Shared.Utilities
global using SharedUtcDateTimeConverter = myIoTGrid.Shared.Utilities.Converters.UtcDateTimeConverter;
global using SharedUtcNullableDateTimeConverter = myIoTGrid.Shared.Utilities.Converters.UtcNullableDateTimeConverter;

namespace myIoTGrid.Hub.Shared;

/// <summary>
/// This file provides type aliases for shared types.
/// Hub-specific code can continue using local types while
/// gradually migrating to use Shared types directly.
/// </summary>
public static class SharedTypeAliases
{
    // Marker class - no implementation needed
}
