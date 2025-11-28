using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DefaultLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IconName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorTypes",
                columns: table => new
                {
                    TypeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ClusterId = table.Column<uint>(type: "INTEGER", nullable: false),
                    MatterClusterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Resolution = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.10000000000000001),
                    MinValue = table.Column<double>(type: "REAL", nullable: true),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsCustom = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, defaultValue: "other"),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorTypes", x => x.TypeId);
                });

            migrationBuilder.CreateTable(
                name: "SyncedNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CloudNodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SourceDetails = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location_Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location_Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Location_Longitude = table.Column<double>(type: "REAL", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncedNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CloudApiKey = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncedReadings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SyncedNodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorTypeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncedReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncedReadings_SensorTypes_SensorTypeId",
                        column: x => x.SensorTypeId,
                        principalTable: "SensorTypes",
                        principalColumn: "TypeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SyncedReadings_SyncedNodes_SyncedNodeId",
                        column: x => x.SyncedNodeId,
                        principalTable: "SyncedNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HubId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hubs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HubId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Location_Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location_Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Location_Longitude = table.Column<double>(type: "REAL", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BatteryLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nodes_Hubs_HubId",
                        column: x => x.HubId,
                        principalTable: "Hubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HubId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    AlertTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Recommendation = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_AlertTypes_AlertTypeId",
                        column: x => x.AlertTypeId,
                        principalTable: "AlertTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alerts_Hubs_HubId",
                        column: x => x.HubId,
                        principalTable: "Hubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Alerts_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Readings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorTypeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSyncedToCloud = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Readings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Readings_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Readings_SensorTypes_SensorTypeId",
                        column: x => x.SensorTypeId,
                        principalTable: "SensorTypes",
                        principalColumn: "TypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sensors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorTypeId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    EndpointId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sensors_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sensors_SensorTypes_SensorTypeId",
                        column: x => x.SensorTypeId,
                        principalTable: "SensorTypes",
                        principalColumn: "TypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertTypeId",
                table: "Alerts",
                column: "AlertTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_CreatedAt",
                table: "Alerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_HubId",
                table: "Alerts",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_IsActive",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Level",
                table: "Alerts",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_NodeId",
                table: "Alerts",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Source",
                table: "Alerts",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId",
                table: "Alerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId_IsActive",
                table: "Alerts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_TenantId_Level_IsActive",
                table: "Alerts",
                columns: new[] { "TenantId", "Level", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AlertTypes_Code",
                table: "AlertTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlertTypes_IsGlobal",
                table: "AlertTypes",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_HubId",
                table: "Hubs",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_IsOnline",
                table: "Hubs",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_LastSeen",
                table: "Hubs",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_TenantId",
                table: "Hubs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_TenantId_HubId",
                table: "Hubs",
                columns: new[] { "TenantId", "HubId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_HubId",
                table: "Nodes",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_HubId_NodeId",
                table: "Nodes",
                columns: new[] { "HubId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_IsOnline",
                table: "Nodes",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_LastSeen",
                table: "Nodes",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_NodeId",
                table: "Nodes",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_IsSyncedToCloud",
                table: "Readings",
                column: "IsSyncedToCloud");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_NodeId",
                table: "Readings",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_NodeId_SensorTypeId_Timestamp",
                table: "Readings",
                columns: new[] { "NodeId", "SensorTypeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_NodeId_Timestamp",
                table: "Readings",
                columns: new[] { "NodeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_SensorTypeId",
                table: "Readings",
                column: "SensorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_TenantId",
                table: "Readings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Readings_TenantId_Timestamp",
                table: "Readings",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Readings_Timestamp",
                table: "Readings",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_IsActive",
                table: "Sensors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_NodeId",
                table: "Sensors",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_NodeId_EndpointId",
                table: "Sensors",
                columns: new[] { "NodeId", "EndpointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_SensorTypeId",
                table: "Sensors",
                column: "SensorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_Category",
                table: "SensorTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_ClusterId",
                table: "SensorTypes",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_IsCustom",
                table: "SensorTypes",
                column: "IsCustom");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_IsGlobal",
                table: "SensorTypes",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedNodes_CloudNodeId",
                table: "SyncedNodes",
                column: "CloudNodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncedNodes_IsOnline",
                table: "SyncedNodes",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedNodes_LastSyncAt",
                table: "SyncedNodes",
                column: "LastSyncAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedNodes_NodeId",
                table: "SyncedNodes",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedNodes_Source",
                table: "SyncedNodes",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_SensorTypeId",
                table: "SyncedReadings",
                column: "SensorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_SyncedAt",
                table: "SyncedReadings",
                column: "SyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_SyncedNodeId",
                table: "SyncedReadings",
                column: "SyncedNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_SyncedNodeId_SensorTypeId_Timestamp",
                table: "SyncedReadings",
                columns: new[] { "SyncedNodeId", "SensorTypeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_SyncedNodeId_Timestamp",
                table: "SyncedReadings",
                columns: new[] { "SyncedNodeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_Timestamp",
                table: "SyncedReadings",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                table: "Tenants",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Readings");

            migrationBuilder.DropTable(
                name: "Sensors");

            migrationBuilder.DropTable(
                name: "SyncedReadings");

            migrationBuilder.DropTable(
                name: "AlertTypes");

            migrationBuilder.DropTable(
                name: "Nodes");

            migrationBuilder.DropTable(
                name: "SensorTypes");

            migrationBuilder.DropTable(
                name: "SyncedNodes");

            migrationBuilder.DropTable(
                name: "Hubs");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
