using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSpecialistStoredMatchingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "availability",
                table: "specialists");

            migrationBuilder.DropColumn(
                name: "case_count",
                table: "specialists");

            migrationBuilder.DropColumn(
                name: "distance_km",
                table: "specialists");

            migrationBuilder.DropColumn(
                name: "success_rate",
                table: "specialists");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "specialists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "availability",
                table: "specialists",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "case_count",
                table: "specialists",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "distance_km",
                table: "specialists",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "success_rate",
                table: "specialists",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "tags",
                table: "specialists",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
