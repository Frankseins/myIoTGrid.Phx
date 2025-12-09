using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHubProvisioningSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApiPort",
                table: "Hubs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ApiUrl",
                table: "Hubs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultWifiPassword",
                table: "Hubs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultWifiSsid",
                table: "Hubs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiPort",
                table: "Hubs");

            migrationBuilder.DropColumn(
                name: "ApiUrl",
                table: "Hubs");

            migrationBuilder.DropColumn(
                name: "DefaultWifiPassword",
                table: "Hubs");

            migrationBuilder.DropColumn(
                name: "DefaultWifiSsid",
                table: "Hubs");
        }
    }
}
