// Global Using-Statements für myIoTGrid.Hub.Interface
// Stellt sicher, dass alle Shared-Typen ohne explizite using-Statements verfügbar sind

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;

// === Shared.Common ===
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

// === Shared.Contracts ===
global using myIoTGrid.Shared.Contracts.Services;

// === Shared.Utilities ===
global using myIoTGrid.Shared.Utilities.Extensions;

// === Typ-Aliases für Disambiguierung ===
// Hub Entity vs Hub Namespace
global using Hub = myIoTGrid.Shared.Common.Entities.Hub;
