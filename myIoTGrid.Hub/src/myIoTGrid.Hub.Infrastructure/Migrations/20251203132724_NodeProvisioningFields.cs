using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NodeProvisioningFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKeyHash",
                table: "Nodes",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MacAddress",
                table: "Nodes",
                type: "TEXT",
                maxLength: 17,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Nodes",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_MacAddress",
                table: "Nodes",
                column: "MacAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Nodes_Status",
                table: "Nodes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Nodes_MacAddress",
                table: "Nodes");

            migrationBuilder.DropIndex(
                name: "IX_Nodes_Status",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "ApiKeyHash",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "MacAddress",
                table: "Nodes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Nodes");
        }
    }
}
