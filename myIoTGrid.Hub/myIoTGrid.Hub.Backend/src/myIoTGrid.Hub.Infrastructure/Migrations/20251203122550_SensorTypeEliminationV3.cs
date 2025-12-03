using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace myIoTGrid.Hub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SensorTypeEliminationV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns to Sensors first (before dropping FK)
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Sensors",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Sensors",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Sensors",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatasheetUrl",
                table: "Sensors",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "I2CAddress",
                table: "Sensors",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Sensors",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IntervalSeconds",
                table: "Sensors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Sensors",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinIntervalSeconds",
                table: "Sensors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Sensors",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Protocol",
                table: "Sensors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WarmupTimeMs",
                table: "Sensors",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Step 2: Migrate data from SensorTypes to Sensors
            migrationBuilder.Sql(@"
                UPDATE Sensors
                SET
                    Code = (SELECT Code || '-' || substr(Sensors.Id, 1, 8) FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    Category = (SELECT Category FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    Protocol = (SELECT Protocol FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    IntervalSeconds = COALESCE(Sensors.IntervalSecondsOverride, (SELECT DefaultIntervalSeconds FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId), 60),
                    MinIntervalSeconds = (SELECT MinIntervalSeconds FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    WarmupTimeMs = (SELECT WarmupTimeMs FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    I2CAddress = COALESCE(Sensors.I2CAddressOverride, (SELECT DefaultI2CAddress FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId)),
                    Manufacturer = (SELECT Manufacturer FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    Icon = (SELECT Icon FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    Color = (SELECT Color FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId),
                    DatasheetUrl = (SELECT DatasheetUrl FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId)
                WHERE EXISTS (SELECT 1 FROM SensorTypes WHERE SensorTypes.Id = Sensors.SensorTypeId);
            ");

            // Set default values for any remaining NULLs
            migrationBuilder.Sql(@"
                UPDATE Sensors SET Code = 'sensor-' || substr(Id, 1, 8) WHERE Code IS NULL OR Code = '';
                UPDATE Sensors SET Category = 'climate' WHERE Category IS NULL OR Category = '';
            ");

            // Step 3: Create SensorCapabilities table
            migrationBuilder.CreateTable(
                name: "SensorCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    MeasurementType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    MinValue = table.Column<double>(type: "REAL", nullable: true),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    Resolution = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.01),
                    Accuracy = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.5),
                    MatterClusterId = table.Column<uint>(type: "INTEGER", nullable: true),
                    MatterClusterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorCapabilities_Sensors_SensorId",
                        column: x => x.SensorId,
                        principalTable: "Sensors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 4: Migrate SensorTypeCapabilities to SensorCapabilities
            migrationBuilder.Sql(@"
                INSERT INTO SensorCapabilities (Id, SensorId, MeasurementType, DisplayName, Unit, MinValue, MaxValue, Resolution, Accuracy, MatterClusterId, MatterClusterName, SortOrder, IsActive)
                SELECT
                    lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-4' || substr(hex(randomblob(2)),2) || '-' || substr('89ab',abs(random()) % 4 + 1, 1) || substr(hex(randomblob(2)),2) || '-' || hex(randomblob(6))),
                    s.Id,
                    stc.MeasurementType,
                    stc.DisplayName,
                    stc.Unit,
                    stc.MinValue,
                    stc.MaxValue,
                    stc.Resolution,
                    stc.Accuracy,
                    stc.MatterClusterId,
                    stc.MatterClusterName,
                    stc.SortOrder,
                    stc.IsActive
                FROM SensorTypeCapabilities stc
                INNER JOIN SensorTypes st ON stc.SensorTypeId = st.Id
                INNER JOIN Sensors s ON s.SensorTypeId = st.Id;
            ");

            // Step 5: Now drop the FK constraint and old columns/tables
            migrationBuilder.DropForeignKey(
                name: "FK_Sensors_SensorTypes_SensorTypeId",
                table: "Sensors");

            migrationBuilder.DropTable(
                name: "SensorTypeCapabilities");

            migrationBuilder.DropTable(
                name: "SensorTypes");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_SensorTypeId",
                table: "Sensors");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_TenantId_SensorTypeId",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "ActiveCapabilityIdsJson",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "AnalogPinOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "I2CAddressOverride",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "SensorTypeId",
                table: "Sensors");

            migrationBuilder.RenameColumn(
                name: "SensorTypeId",
                table: "SyncedReadings",
                newName: "SensorCode");

            migrationBuilder.RenameIndex(
                name: "IX_SyncedReadings_SyncedNodeId_SensorTypeId_Timestamp",
                table: "SyncedReadings",
                newName: "IX_SyncedReadings_SyncedNodeId_SensorCode_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_SyncedReadings_SensorTypeId",
                table: "SyncedReadings",
                newName: "IX_SyncedReadings_SensorCode");

            migrationBuilder.RenameColumn(
                name: "TriggerPinOverride",
                table: "Sensors",
                newName: "TriggerPin");

            migrationBuilder.RenameColumn(
                name: "SdaPinOverride",
                table: "Sensors",
                newName: "SdaPin");

            migrationBuilder.RenameColumn(
                name: "SclPinOverride",
                table: "Sensors",
                newName: "SclPin");

            migrationBuilder.RenameColumn(
                name: "OneWirePinOverride",
                table: "Sensors",
                newName: "OneWirePin");

            migrationBuilder.RenameColumn(
                name: "IntervalSecondsOverride",
                table: "Sensors",
                newName: "EchoPin");

            migrationBuilder.RenameColumn(
                name: "EchoPinOverride",
                table: "Sensors",
                newName: "DigitalPin");

            migrationBuilder.RenameColumn(
                name: "DigitalPinOverride",
                table: "Sensors",
                newName: "AnalogPin");

            // Step 6: Add columns to SyncedReadings
            migrationBuilder.AddColumn<string>(
                name: "MeasurementType",
                table: "SyncedReadings",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "SyncedReadings",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Step 7: Make Code and Category NOT NULL after data migration
            // Note: These columns were already added as nullable in Step 1, now we're done with data migration
            // SQLite doesn't support ALTER COLUMN, so we just leave them as-is (they have values now)

            // Step 8: Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_SyncedReadings_MeasurementType",
                table: "SyncedReadings",
                column: "MeasurementType");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_Category",
                table: "Sensors",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_Code",
                table: "Sensors",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_Protocol",
                table: "Sensors",
                column: "Protocol");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_TenantId_Code",
                table: "Sensors",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorCapabilities_MatterClusterId",
                table: "SensorCapabilities",
                column: "MatterClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorCapabilities_MeasurementType",
                table: "SensorCapabilities",
                column: "MeasurementType");

            migrationBuilder.CreateIndex(
                name: "IX_SensorCapabilities_SensorId",
                table: "SensorCapabilities",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorCapabilities_SensorId_MeasurementType",
                table: "SensorCapabilities",
                columns: new[] { "SensorId", "MeasurementType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SensorCapabilities");

            migrationBuilder.DropIndex(
                name: "IX_SyncedReadings_MeasurementType",
                table: "SyncedReadings");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_Category",
                table: "Sensors");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_Code",
                table: "Sensors");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_Protocol",
                table: "Sensors");

            migrationBuilder.DropIndex(
                name: "IX_Sensors_TenantId_Code",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "MeasurementType",
                table: "SyncedReadings");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "SyncedReadings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "DatasheetUrl",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "I2CAddress",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "IntervalSeconds",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "MinIntervalSeconds",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "Protocol",
                table: "Sensors");

            migrationBuilder.DropColumn(
                name: "WarmupTimeMs",
                table: "Sensors");

            migrationBuilder.RenameColumn(
                name: "SensorCode",
                table: "SyncedReadings",
                newName: "SensorTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_SyncedReadings_SyncedNodeId_SensorCode_Timestamp",
                table: "SyncedReadings",
                newName: "IX_SyncedReadings_SyncedNodeId_SensorTypeId_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_SyncedReadings_SensorCode",
                table: "SyncedReadings",
                newName: "IX_SyncedReadings_SensorTypeId");

            migrationBuilder.RenameColumn(
                name: "TriggerPin",
                table: "Sensors",
                newName: "TriggerPinOverride");

            migrationBuilder.RenameColumn(
                name: "SdaPin",
                table: "Sensors",
                newName: "SdaPinOverride");

            migrationBuilder.RenameColumn(
                name: "SclPin",
                table: "Sensors",
                newName: "SclPinOverride");

            migrationBuilder.RenameColumn(
                name: "OneWirePin",
                table: "Sensors",
                newName: "OneWirePinOverride");

            migrationBuilder.RenameColumn(
                name: "EchoPin",
                table: "Sensors",
                newName: "IntervalSecondsOverride");

            migrationBuilder.RenameColumn(
                name: "DigitalPin",
                table: "Sensors",
                newName: "EchoPinOverride");

            migrationBuilder.RenameColumn(
                name: "AnalogPin",
                table: "Sensors",
                newName: "DigitalPinOverride");

            migrationBuilder.AddColumn<string>(
                name: "ActiveCapabilityIdsJson",
                table: "Sensors",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AnalogPinOverride",
                table: "Sensors",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "I2CAddressOverride",
                table: "Sensors",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SensorTypeId",
                table: "Sensors",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SensorTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DatasheetUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DefaultAnalogPin = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultDigitalPin = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultEchoPin = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultGainCorrection = table.Column<double>(type: "REAL", nullable: false, defaultValue: 1.0),
                    DefaultI2CAddress = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DefaultIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 60),
                    DefaultOffsetCorrection = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.0),
                    DefaultOneWirePin = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultSclPin = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultSdaPin = table.Column<int>(type: "INTEGER", nullable: true),
                    DefaultTriggerPin = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsGlobal = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Manufacturer = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MinIntervalSeconds = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Protocol = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WarmupTimeMs = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorTypeCapabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SensorTypeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Accuracy = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.5),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    MatterClusterId = table.Column<uint>(type: "INTEGER", nullable: true),
                    MatterClusterName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MaxValue = table.Column<double>(type: "REAL", nullable: true),
                    MeasurementType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    MinValue = table.Column<double>(type: "REAL", nullable: true),
                    Resolution = table.Column<double>(type: "REAL", nullable: false, defaultValue: 0.01),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorTypeCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SensorTypeCapabilities_SensorTypes_SensorTypeId",
                        column: x => x.SensorTypeId,
                        principalTable: "SensorTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_SensorTypeId",
                table: "Sensors",
                column: "SensorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sensors_TenantId_SensorTypeId",
                table: "Sensors",
                columns: new[] { "TenantId", "SensorTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypeCapabilities_MatterClusterId",
                table: "SensorTypeCapabilities",
                column: "MatterClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypeCapabilities_MeasurementType",
                table: "SensorTypeCapabilities",
                column: "MeasurementType");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypeCapabilities_SensorTypeId",
                table: "SensorTypeCapabilities",
                column: "SensorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypeCapabilities_SensorTypeId_MeasurementType",
                table: "SensorTypeCapabilities",
                columns: new[] { "SensorTypeId", "MeasurementType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_Category",
                table: "SensorTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_Code",
                table: "SensorTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_IsActive",
                table: "SensorTypes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_IsGlobal",
                table: "SensorTypes",
                column: "IsGlobal");

            migrationBuilder.CreateIndex(
                name: "IX_SensorTypes_Protocol",
                table: "SensorTypes",
                column: "Protocol");

            migrationBuilder.AddForeignKey(
                name: "FK_Sensors_SensorTypes_SensorTypeId",
                table: "Sensors",
                column: "SensorTypeId",
                principalTable: "SensorTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
