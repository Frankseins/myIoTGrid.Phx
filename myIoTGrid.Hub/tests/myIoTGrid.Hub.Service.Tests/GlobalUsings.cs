// Global using directives for myIoTGrid.Hub.Service.Tests
// Provides backwards compatibility after migration to Shared Libraries

// Shared.Common - Entities, DTOs, Enums, etc.
global using myIoTGrid.Shared.Common.Entities;
global using myIoTGrid.Shared.Common.Enums;
global using myIoTGrid.Shared.Common.Interfaces;
global using myIoTGrid.Shared.Common.ValueObjects;
global using myIoTGrid.Shared.Common.Options;
global using myIoTGrid.Shared.Common.DTOs;
global using myIoTGrid.Shared.Common.DTOs.Chart;
global using myIoTGrid.Shared.Common.DTOs.Dashboard;
global using myIoTGrid.Shared.Common.DTOs.Common;
global using myIoTGrid.Shared.Common.DTOs.Discovery;
global using myIoTGrid.Shared.Common.Constants;

// Shared.Contracts - Service Interfaces
global using myIoTGrid.Shared.Contracts.Services;

// Shared.Utilities - Extensions
global using myIoTGrid.Shared.Utilities.Extensions;
global using myIoTGrid.Shared.Utilities.Converters;

// Hub.Infrastructure
global using myIoTGrid.Hub.Infrastructure.Data;
global using myIoTGrid.Hub.Infrastructure.Repositories;

// Hub.Service
global using myIoTGrid.Hub.Service.Services;
global using myIoTGrid.Hub.Service.Extensions;

// Type alias for Hub entity (disambiguation)
global using Hub = myIoTGrid.Shared.Common.Entities.Hub;
global using HubEntity = myIoTGrid.Shared.Common.Entities.Hub;
