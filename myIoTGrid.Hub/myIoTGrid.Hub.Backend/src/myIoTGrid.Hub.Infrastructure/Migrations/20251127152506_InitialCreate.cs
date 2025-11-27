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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IconName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorTypes", x => x.Id);
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
                name: "SensorHubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    HubId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Protocol = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultLocation_Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DefaultLocation_Latitude = table.Column<double>(type: "REAL", nullable: true),
                    DefaultLocation_Longitude = table.Column<double>(type: "REAL", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorHubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorHubs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorHubId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                        name: "FK_Alerts_SensorHubs_SensorHubId",
                        column: x => x.SensorHubId,
                        principalTable: "SensorHubs",
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
                name: "SensorData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorHubId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Location_Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location_Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Location_Longitude = table.Column<double>(type: "REAL", nullable: true),
                    IsSyncedToCloud = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorData_SensorHubs_SensorHubId",
                        column: x => x.SensorHubId,
                        principalTable: "SensorHubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SensorData_SensorTypes_SensorTypeId",
                        column: x => x.SensorTypeId,
                        principalTable: "SensorTypes",
                        principalColumn: "Id",
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
                name: "IX_Alerts_IsActive",
                table: "Alerts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Level",
                table: "Alerts",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_SensorHubId",
                table: "Alerts",
                column: "SensorHubId");

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
                name: "IX_SensorData_IsSyncedToCloud",
                table: "SensorData",
                column: "IsSyncedToCloud");

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_SensorHubId",
                table: "SensorData",
                column: "SensorHubId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_SensorHubId_SensorTypeId_Timestamp",
                table: "SensorData",
                columns: new[] { "SensorHubId", "SensorTypeId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_SensorHubId_Timestamp",
                table: "SensorData",
                columns: new[] { "SensorHubId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_SensorTypeId",
                table: "SensorData",
                column: "SensorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_TenantId",
                table: "SensorData",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_TenantId_Timestamp",
                table: "SensorData",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorData_Timestamp",
                table: "SensorData",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SensorHubs_HubId",
                table: "SensorHubs",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorHubs_IsOnline",
                table: "SensorHubs",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_SensorHubs_LastSeen",
                table: "SensorHubs",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_SensorHubs_TenantId",
                table: "SensorHubs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorHubs_TenantId_HubId",
                table: "SensorHubs",
                columns: new[] { "TenantId", "HubId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_Code",
                table: "SensorTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_IsGlobal",
                table: "SensorTypes",
                column: "IsGlobal");

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
                name: "SensorData");

            migrationBuilder.DropTable(
                name: "AlertTypes");

            migrationBuilder.DropTable(
                name: "SensorHubs");

            migrationBuilder.DropTable(
                name: "SensorTypes");

            migrationBuilder.DropTable(
                name: "Tenants");
        }
    }
}
