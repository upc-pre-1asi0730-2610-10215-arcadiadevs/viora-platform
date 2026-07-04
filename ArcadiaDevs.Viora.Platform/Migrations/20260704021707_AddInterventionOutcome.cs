using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddInterventionOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "intervention_outcomes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    intervention_execution_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    grace_period = table.Column<string>(type: "text", nullable: false),
                    observed_result = table.Column<string>(type: "text", nullable: false),
                    impact_level = table.Column<string>(type: "text", nullable: false),
                    producer_assessment = table.Column<string>(type: "text", nullable: false),
                    service_result = table.Column<string>(type: "text", nullable: true),
                    hire_again = table.Column<bool>(type: "boolean", nullable: true),
                    private_feedback = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_intervention_outcomes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_intervention_outcomes_intervention_execution_id",
                table: "intervention_outcomes",
                column: "intervention_execution_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "intervention_outcomes");
        }
    }
}
