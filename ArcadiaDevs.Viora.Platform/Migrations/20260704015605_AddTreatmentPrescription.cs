using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddTreatmentPrescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "treatment_prescriptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_proposal_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    finding_type = table.Column<string>(type: "text", nullable: true),
                    incidence_level = table.Column<string>(type: "text", nullable: true),
                    technical_description = table.Column<string>(type: "text", nullable: true),
                    record_date = table.Column<DateOnly>(type: "date", nullable: true),
                    application_method = table.Column<string>(type: "text", nullable: true),
                    spray_volume = table.Column<string>(type: "text", nullable: true),
                    pre_harvest_interval = table.Column<string>(type: "text", nullable: true),
                    agronomist_recommendations = table.Column<string>(type: "text", nullable: true),
                    required_ppe = table.Column<string>(type: "text", nullable: true),
                    products = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_treatment_prescriptions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_treatment_prescriptions_service_proposal_id",
                table: "treatment_prescriptions",
                column: "service_proposal_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "treatment_prescriptions");
        }
    }
}
