using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_proposals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    intervention_request_id = table.Column<int>(type: "integer", nullable: false),
                    specialist_id = table.Column<int>(type: "integer", nullable: false),
                    service_title = table.Column<string>(type: "text", nullable: false),
                    duration_label = table.Column<string>(type: "text", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false),
                    proposed_date = table.Column<DateOnly>(type: "date", nullable: false),
                    cost_estimate_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cost_estimate_currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    proposal_details = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_service_proposals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_service_proposals_intervention_request_id",
                table: "service_proposals",
                column: "intervention_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_proposals");
        }
    }
}
