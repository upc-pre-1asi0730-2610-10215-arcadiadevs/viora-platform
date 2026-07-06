using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileMarketplaceAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "service_radius_km",
                table: "profiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "service_tags",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "availability",
                table: "profiles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "show_pro_badge",
                table: "profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "latitude",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "service_radius_km",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "service_tags",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "availability",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "show_pro_badge",
                table: "profiles");
        }
    }
}
