using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agronomic_agro_monitoring_plot_integrations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plot_id = table.Column<int>(type: "integer", nullable: false),
                    external_polygon_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    boundary_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_imagery_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tile_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    capture_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ndvi_mean = table.Column<double>(type: "double precision", nullable: true),
                    cloud_percentage = table.Column<double>(type: "double precision", nullable: true),
                    last_checked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_agronomic_agro_monitoring_plot_integrations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alerts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plot_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    risk_explanation = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    sources = table.Column<string>(type: "text", nullable: false),
                    data_providers = table.Column<string>(type: "text", nullable: false),
                    supporting_data = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_alerts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dynamic_nutrition_plans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    plot_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    application_window_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    application_window_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rationale_summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    triggering_risk_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ndvi_value = table.Column<double>(type: "numeric(4,2)", nullable: false),
                    temperature_anomaly = table.Column<double>(type: "numeric(5,2)", nullable: false),
                    generated_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    application_date = table.Column<DateOnly>(type: "date", nullable: true),
                    application_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    applied_inputs = table.Column<string>(type: "text", nullable: true),
                    dose_confirmation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    field_operator = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    field_notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_dynamic_nutrition_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "iot_devices",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plot_id = table.Column<long>(type: "bigint", nullable: false),
                    device_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_iot_devices", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pest_sighting_reports",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    plot_id = table.Column<long>(type: "bigint", nullable: false),
                    reporter_user_id = table.Column<long>(type: "bigint", nullable: false),
                    risk_zone = table.Column<string>(type: "text", nullable: false),
                    symptoms = table.Column<string>(type: "text", nullable: false),
                    observed_severity = table.Column<string>(type: "text", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    evaluated = table.Column<bool>(type: "boolean", nullable: false),
                    calculated_risk = table.Column<string>(type: "text", nullable: false),
                    probable_threat = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    alert_confirmed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_pest_sighting_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plots",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_user_id = table.Column<int>(type: "integer", nullable: false),
                    plot_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    crop_type = table.Column<string>(type: "text", nullable: true),
                    variety = table.Column<string>(type: "text", nullable: true),
                    location = table.Column<string>(type: "text", nullable: true),
                    campaign = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    polygon_coordinates = table.Column<string>(type: "jsonb", nullable: false),
                    area_size = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    agro_monitoring_polygon_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    agro_monitoring_center = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    chill_requirement_portions = table.Column<double>(type: "numeric(18,6)", nullable: true),
                    chill_requirement_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    chill_requirement_model = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_plots", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "symptom_dictionary_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    description_en = table.Column<string>(type: "text", nullable: false),
                    description_es = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_symptom_dictionary_items", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "alert_timeline_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    alert_id = table.Column<long>(type: "bigint", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_alert_timeline_records", x => x.id);
                    table.ForeignKey(
                        name: "f_k_alert_timeline_records_alerts_alert_id",
                        column: x => x.alert_id,
                        principalTable: "alerts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dynamic_nutrition_plan_inputs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    purpose = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    dosage = table.Column<double>(type: "numeric(10,2)", nullable: false),
                    dosage_unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    dynamic_nutrition_plan_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_dynamic_nutrition_plan_inputs", x => x.id);
                    table.ForeignKey(
                        name: "f_k_dynamic_nutrition_plan_inputs_dynamic_nutrition_plans_dynam~",
                        column: x => x.dynamic_nutrition_plan_id,
                        principalTable: "dynamic_nutrition_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_agronomic_agro_monitoring_plot_integrations_plot_id",
                table: "agronomic_agro_monitoring_plot_integrations",
                column: "plot_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_alert_timeline_records_alert_id",
                table: "alert_timeline_records",
                column: "alert_id");

            migrationBuilder.CreateIndex(
                name: "i_x_dynamic_nutrition_plan_inputs_dynamic_nutrition_plan_id",
                table: "dynamic_nutrition_plan_inputs",
                column: "dynamic_nutrition_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_iot_devices_id_plot_id",
                table: "iot_devices",
                columns: new[] { "id", "plot_id" });

            migrationBuilder.CreateIndex(
                name: "ix_iot_devices_plot_id",
                table: "iot_devices",
                column: "plot_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agronomic_agro_monitoring_plot_integrations");

            migrationBuilder.DropTable(
                name: "alert_timeline_records");

            migrationBuilder.DropTable(
                name: "dynamic_nutrition_plan_inputs");

            migrationBuilder.DropTable(
                name: "iot_devices");

            migrationBuilder.DropTable(
                name: "pest_sighting_reports");

            migrationBuilder.DropTable(
                name: "plots");

            migrationBuilder.DropTable(
                name: "symptom_dictionary_items");

            migrationBuilder.DropTable(
                name: "alerts");

            migrationBuilder.DropTable(
                name: "dynamic_nutrition_plans");
        }
    }
}
