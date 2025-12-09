using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteDebugSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DebugLevel",
                table: "Nodes",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<bool>(
                name: "EnableRemoteLogging",
                table: "Nodes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDebugChange",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NodeDebugLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NodeTimestamp = table.Column<long>(type: "INTEGER", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    StackTrace = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeDebugLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeDebugLogs_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_Category",
                table: "NodeDebugLogs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_Level",
                table: "NodeDebugLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_NodeId",
                table: "NodeDebugLogs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_NodeId_Category_ReceivedAt",
                table: "NodeDebugLogs",
                columns: new[] { "NodeId", "Category", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_NodeId_Level_ReceivedAt",
                table: "NodeDebugLogs",
                columns: new[] { "NodeId", "Level", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_NodeId_ReceivedAt",
                table: "NodeDebugLogs",
                columns: new[] { "NodeId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeDebugLogs_ReceivedAt",
                table: "NodeDebugLogs",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeDebugLogs");

            migrationBuilder.DropColumn(
                name: "DebugLevel",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "EnableRemoteLogging",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "LastDebugChange",
                table: "Nodes");
        }
    }
}
