using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddInterventionExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "intervention_executions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    treatment_prescription_id = table.Column<int>(type: "integer", nullable: false),
                    application_date = table.Column<DateOnly>(type: "date", nullable: false),
                    applied_area = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    execution_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    field_note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_intervention_executions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_intervention_executions_treatment_prescription_id",
                table: "intervention_executions",
                column: "treatment_prescription_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "intervention_executions");
        }
    }
}
