using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddMonitoringSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "monitoring_summaries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    general_health_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    average_ndvi = table.Column<decimal>(type: "numeric(4,2)", nullable: false),
                    accumulated_chill_hours = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    yield_projection = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    last_synchronization_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    mitigation_action_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mitigation_recommended_application_window = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    mitigation_suggested_inputs = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    weather_climate_risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    weather_current_temperature = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    weather_last_validated_reading_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    weather_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_monitoring_summaries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_monitoring_summaries_user_id",
                table: "monitoring_summaries",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "monitoring_summaries");
        }
    }
}
