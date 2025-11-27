using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint31_SensorEntityRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_SensorHubs_SensorHubId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorData_SensorHubs_SensorHubId",
                table: "SensorData");

            migrationBuilder.DropTable(
                name: "SensorHubs");

            migrationBuilder.DropColumn(
                name: "Location_Latitude",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "Location_Longitude",
                table: "SensorData");

            migrationBuilder.DropColumn(
                name: "Location_Name",
                table: "SensorData");

            migrationBuilder.RenameColumn(
                name: "SensorHubId",
                table: "SensorData",
                newName: "SensorId");

            migrationBuilder.RenameIndex(
                name: "IX_SensorData_SensorHubId_Timestamp",
                table: "SensorData",
                newName: "IX_SensorData_SensorId_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_SensorData_SensorHubId_SensorTypeId_Timestamp",
                table: "SensorData",
                newName: "IX_SensorData_SensorId_SensorTypeId_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_SensorData_SensorHubId",
                table: "SensorData",
                newName: "IX_SensorData_SensorId");

            migrationBuilder.RenameColumn(
                name: "SensorHubId",
                table: "Alerts",
                newName: "SensorId");

            migrationBuilder.RenameIndex(
                name: "IX_Alerts_SensorHubId",
                table: "Alerts",
                newName: "IX_Alerts_SensorId");

            migrationBuilder.AddColumn<Guid>(
                name: "HubId",
                table: "Alerts",
                type: "TEXT",
                nullable: true);

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
                name: "Sensors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HubId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Location_Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Location_Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Location_Longitude = table.Column<double>(type: "REAL", nullable: true),
                    SensorTypes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    FirmwareVersion = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BatteryLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sensors_Hubs_HubId",
                        column: x => x.HubId,
                        principalTable: "Hubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_HubId",
                table: "Alerts",
                column: "HubId");

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
                name: "IX_Sensors_HubId",
                table: "Sensors",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_HubId_SensorId",
                table: "Sensors",
                columns: new[] { "HubId", "SensorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_IsOnline",
                table: "Sensors",
                column: "IsOnline");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_LastSeen",
                table: "Sensors",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_SensorId",
                table: "Sensors",
                column: "SensorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Hubs_HubId",
                table: "Alerts",
                column: "HubId",
                principalTable: "Hubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_Sensors_SensorId",
                table: "Alerts",
                column: "SensorId",
                principalTable: "Sensors",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SensorData_Sensors_SensorId",
                table: "SensorData",
                column: "SensorId",
                principalTable: "Sensors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Hubs_HubId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_Alerts_Sensors_SensorId",
                table: "Alerts");

            migrationBuilder.DropForeignKey(
                name: "FK_SensorData_Sensors_SensorId",
                table: "SensorData");

            migrationBuilder.DropTable(
                name: "Sensors");

            migrationBuilder.DropTable(
                name: "Hubs");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_HubId",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "HubId",
                table: "Alerts");

            migrationBuilder.RenameColumn(
                name: "SensorId",
                table: "SensorData",
                newName: "SensorHubId");

            migrationBuilder.RenameIndex(
                name: "IX_SensorData_SensorId_Timestamp",
                table: "SensorData",
                newName: "IX_SensorData_SensorHubId_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_SensorData_SensorId_SensorTypeId_Timestamp",
                table: "SensorData",
                newName: "IX_SensorData_SensorHubId_SensorTypeId_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_SensorData_SensorId",
                table: "SensorData",
                newName: "IX_SensorData_SensorHubId");

            migrationBuilder.RenameColumn(
                name: "SensorId",
                table: "Alerts",
                newName: "SensorHubId");

            migrationBuilder.RenameIndex(
                name: "IX_Alerts_SensorId",
                table: "Alerts",
                newName: "IX_Alerts_SensorHubId");

            migrationBuilder.AddColumn<double>(
                name: "Location_Latitude",
                table: "SensorData",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Location_Longitude",
                table: "SensorData",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location_Name",
                table: "SensorData",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SensorHubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HubId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsOnline = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Protocol = table.Column<int>(type: "INTEGER", nullable: false),
                    DefaultLocation_Latitude = table.Column<double>(type: "REAL", nullable: true),
                    DefaultLocation_Longitude = table.Column<double>(type: "REAL", nullable: true),
                    DefaultLocation_Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
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

            migrationBuilder.AddForeignKey(
                name: "FK_Alerts_SensorHubs_SensorHubId",
                table: "Alerts",
                column: "SensorHubId",
                principalTable: "SensorHubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SensorData_SensorHubs_SensorHubId",
                table: "SensorData",
                column: "SensorHubId",
                principalTable: "SensorHubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
