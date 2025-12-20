using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBluetoothHub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BluetoothHubId",
                table: "Nodes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BluetoothHubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HubId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MacAddress = table.Column<string>(type: "TEXT", maxLength: 17, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false, defaultValue: "Inactive"),
                    LastSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BluetoothHubs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BluetoothHubs_Hubs_HubId",
                        column: x => x.HubId,
                        principalTable: "Hubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_BluetoothHubId",
                table: "Nodes",
                column: "BluetoothHubId");

            migrationBuilder.CreateIndex(
                name: "IX_BluetoothHubs_HubId",
                table: "BluetoothHubs",
                column: "HubId");

            migrationBuilder.CreateIndex(
                name: "IX_BluetoothHubs_LastSeen",
                table: "BluetoothHubs",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_BluetoothHubs_MacAddress",
                table: "BluetoothHubs",
                column: "MacAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BluetoothHubs_Status",
                table: "BluetoothHubs",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Nodes_BluetoothHubs_BluetoothHubId",
                table: "Nodes",
                column: "BluetoothHubId",
                principalTable: "BluetoothHubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Nodes_BluetoothHubs_BluetoothHubId",
                table: "Nodes");

            migrationBuilder.DropTable(
                name: "BluetoothHubs");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_BluetoothHubId",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "BluetoothHubId",
                table: "Nodes");
        }
    }
}
