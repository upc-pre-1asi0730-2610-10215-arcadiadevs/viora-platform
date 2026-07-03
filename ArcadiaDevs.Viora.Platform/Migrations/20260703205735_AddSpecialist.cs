using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecialist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "specialists",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    profile_user_id = table.Column<int>(type: "integer", nullable: false),
                    success_rate = table.Column<double>(type: "double precision", nullable: false),
                    case_count = table.Column<int>(type: "integer", nullable: false),
                    distance_km = table.Column<double>(type: "double precision", nullable: false),
                    tags = table.Column<string>(type: "text", nullable: false),
                    availability = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    whatsapp = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_specialists", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_specialists_profile_user_id",
                table: "specialists",
                column: "profile_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "specialists");
        }
    }
}
