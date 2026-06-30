using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddIoTDeviceActivationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A4 part 2: nullable activation_code column on iot_devices. Existing
            // rows pre-dating the catalog keep NULL; new devices are claimed via
            // IoTDeviceCommandService.Handle(CreateIoTDeviceCommand) which writes
            // a non-null code.
            migrationBuilder.AddColumn<string>(
                name: "activation_code",
                table: "iot_devices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Unique index defending against double-claim races that slip past
            // the pre-flight ExistsByActivationCodeAsync check. PostgreSQL's
            // UNIQUE index treats NULLs as distinct, so multiple legacy rows
            // with NULL activation_code are allowed.
            migrationBuilder.CreateIndex(
                name: "ix_iot_devices_activation_code",
                table: "iot_devices",
                column: "activation_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_iot_devices_activation_code",
                table: "iot_devices");

            migrationBuilder.DropColumn(
                name: "activation_code",
                table: "iot_devices");
        }
    }
}
