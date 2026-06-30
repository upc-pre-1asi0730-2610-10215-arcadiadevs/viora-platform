using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddAgronomicStatistic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agronomic_statistics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    plot_id = table.Column<long>(type: "bigint", nullable: false),
                    measurement_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ndvi_value = table.Column<double>(type: "double precision", nullable: false),
                    chill_portions = table.Column<double>(type: "double precision", nullable: false),
                    chill_hours = table.Column<double>(type: "double precision", nullable: false),
                    chill_model_intermediate_product = table.Column<double>(type: "double precision", nullable: false),
                    chill_model_previous_hour_temperature_celsius = table.Column<double>(type: "double precision", nullable: true),
                    chill_model_prior_hour_temperature_celsius = table.Column<double>(type: "double precision", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_agronomic_statistics", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agronomic_statistics_plot_id_measurement_date",
                table: "agronomic_statistics",
                columns: new[] { "plot_id", "measurement_date" });

            migrationBuilder.CreateIndex(
                name: "ix_agronomic_statistics_user_id",
                table: "agronomic_statistics",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agronomic_statistics");
        }
    }
}
