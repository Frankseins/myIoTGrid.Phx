using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSensorPinOverrideFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AnalogPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DigitalPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EchoPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "I2CAddressOverride",
                table: "Sensors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OneWirePinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SclPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SdaPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriggerPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnalogPinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "DigitalPinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "EchoPinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "I2CAddressOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "OneWirePinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "SclPinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "SdaPinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "TriggerPinOverride",
                table: "Sensors");
        }
    }
}
