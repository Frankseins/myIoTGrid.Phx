using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpeditionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expeditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    NodeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Planned"),
                    TotalDistanceKm = table.Column<double>(type: "REAL", nullable: true),
                    TotalReadings = table.Column<int>(type: "INTEGER", nullable: true),
                    AverageSpeedKmh = table.Column<double>(type: "REAL", nullable: true),
                    MaxSpeedKmh = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false, defaultValue: "[]"),
                    CoverImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expeditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expeditions_Nodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_CreatedAt",
                table: "Expeditions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_NodeId",
                table: "Expeditions",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_NodeId_StartTime",
                table: "Expeditions",
                columns: new[] { "NodeId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_StartTime",
                table: "Expeditions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Expeditions_Status",
                table: "Expeditions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expeditions");
        }
    }
}
