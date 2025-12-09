using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeReadingAssignmentIdOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Readings_NodeSensorAssignments_AssignmentId",
                table: "Readings");

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignmentId",
                table: "Readings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_Readings_NodeSensorAssignments_AssignmentId",
                table: "Readings",
                column: "AssignmentId",
                principalTable: "NodeSensorAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Readings_NodeSensorAssignments_AssignmentId",
                table: "Readings");

            migrationBuilder.AlterColumn<Guid>(
                name: "AssignmentId",
                table: "Readings",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Readings_NodeSensorAssignments_AssignmentId",
                table: "Readings",
                column: "AssignmentId",
                principalTable: "NodeSensorAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
