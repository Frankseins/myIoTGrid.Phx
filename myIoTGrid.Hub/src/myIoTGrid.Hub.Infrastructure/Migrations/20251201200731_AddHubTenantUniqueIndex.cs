using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHubTenantUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hubs_TenantId",
                table: "Hubs");

            migrationBuilder.DropIndex(
                name: "IX_Hubs_TenantId_HubId",
                table: "Hubs");

            migrationBuilder.CreateIndex(
                name: "IX_Hub_TenantId_Unique",
                table: "Hubs",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Hub_TenantId_Unique",
                table: "Hubs");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_TenantId",
                table: "Hubs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Hubs_TenantId_HubId",
                table: "Hubs",
                columns: new[] { "TenantId", "HubId" },
                unique: true);
        }
    }
}
