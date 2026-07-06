using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "referral_codes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_referral_codes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_referral_codes_code",
                table: "referral_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_referral_codes_user_id",
                table: "referral_codes",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "referral_codes");
        }
    }
}