using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Cloud.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseMacAddressLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MacAddress",
                table: "Nodes",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(17)",
                oldMaxLength: 17);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MacAddress",
                table: "Nodes",
                type: "character varying(17)",
                maxLength: 17,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(21)",
                oldMaxLength: 21);
        }
    }
}
