using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddInterventionRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "intervention_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grower_id = table.Column<int>(type: "integer", nullable: false),
                    plot_id = table.Column<long>(type: "bigint", nullable: false),
                    specialist_id = table.Column<int>(type: "integer", nullable: false),
                    alert_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    decline_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_intervention_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_intervention_requests_grower_id",
                table: "intervention_requests",
                column: "grower_id");

            migrationBuilder.CreateIndex(
                name: "i_x_intervention_requests_specialist_id",
                table: "intervention_requests",
                column: "specialist_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "intervention_requests");
        }
    }
}
