using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNodeSyncAndHistoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SyncedAt",
                table: "Readings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CloudSensorId",
                table: "NodeSensorAssignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "NodeSensorAssignments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NodeSensorAssignments_CloudSensorId",
                table: "NodeSensorAssignments",
                column: "CloudSensorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NodeSensorAssignments_CloudSensorId",
                table: "NodeSensorAssignments");

            migrationBuilder.DropColumn(
                name: "SyncedAt",
                table: "Readings");

            migrationBuilder.DropColumn(
                name: "CloudSensorId",
                table: "NodeSensorAssignments");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "NodeSensorAssignments");
        }
    }
}
