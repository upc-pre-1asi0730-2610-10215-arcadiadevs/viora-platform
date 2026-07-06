using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class StructuredAgrochemicalPrescriptionAndScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pre_harvest_interval",
                table: "treatment_prescriptions");

            migrationBuilder.DropColumn(
                name: "spray_volume",
                table: "treatment_prescriptions");

            migrationBuilder.AlterColumn<string>(
                name: "application_method",
                table: "treatment_prescriptions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pre_harvest_interval_days",
                table: "treatment_prescriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "spray_volume_amount",
                table: "treatment_prescriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spray_volume_unit",
                table: "treatment_prescriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pre_harvest_interval_days",
                table: "treatment_prescriptions");

            migrationBuilder.DropColumn(
                name: "spray_volume_amount",
                table: "treatment_prescriptions");

            migrationBuilder.DropColumn(
                name: "spray_volume_unit",
                table: "treatment_prescriptions");

            migrationBuilder.AlterColumn<string>(
                name: "application_method",
                table: "treatment_prescriptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pre_harvest_interval",
                table: "treatment_prescriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "spray_volume",
                table: "treatment_prescriptions",
                type: "text",
                nullable: true);
        }
    }
}
