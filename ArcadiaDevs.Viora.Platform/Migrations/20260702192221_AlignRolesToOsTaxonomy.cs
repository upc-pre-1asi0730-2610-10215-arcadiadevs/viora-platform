using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArcadiaDevs.Viora.Platform.Migrations
{
    /// <summary>
    ///     Converges the <c>roles</c> table to os-viora-platform's exact
    ///     2-role taxonomy (design Decision 1, obs #155): renames the 2
    ///     surviving roles to their OS-aligned PascalCase names and removes
    ///     the WA-only <c>Administrator</c> role entirely. Roles are
    ///     runtime-seeded (not <c>HasData</c>), so this uses stateful raw
    ///     SQL rather than a model diff. Column names are lowercase
    ///     (<c>roles.name</c>) because <c>AppDbContext</c> uses
    ///     <c>UseSnakeCaseNamingConvention()</c>.
    /// </summary>
    public partial class AlignRolesToOsTaxonomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"roles\" SET \"name\" = 'Grower' WHERE \"name\" = 'OliveProducer';");
            migrationBuilder.Sql("UPDATE \"roles\" SET \"name\" = 'Specialist' WHERE \"name\" = 'PhytosanitarySpecialist';");
            // Deleting by name (not by a hardcoded id, which is identity-generated).
            // f_k_user_roles_roles_roles_id is onDelete: Cascade (20260625052534_AddRoles),
            // so any user_roles rows referencing Administrator are cascade-deleted here too.
            migrationBuilder.Sql("DELETE FROM \"roles\" WHERE \"name\" = 'Administrator';");
        }

        /// <summary>
        ///     Reverses the 2 renames and re-inserts the <c>Administrator</c>
        ///     role row (with a freshly generated identity id).
        ///     <para>
        ///         Accepted data loss: this does NOT restore any
        ///         <c>user_roles</c> links that existed before <see cref="Up"/>
        ///         ran — those rows were cascade-deleted along with the
        ///         original row and cannot be recovered. For any user
        ///         previously assigned <c>Administrator</c>, rollback restores
        ///         the role row but not their assignment. Accepted for this
        ///         academic project (design Decision 1, obs #155) — no
        ///         production users currently hold <c>Administrator</c> in
        ///         tracked environments.
        ///     </para>
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"roles\" SET \"name\" = 'OliveProducer' WHERE \"name\" = 'Grower';");
            migrationBuilder.Sql("UPDATE \"roles\" SET \"name\" = 'PhytosanitarySpecialist' WHERE \"name\" = 'Specialist';");
            migrationBuilder.Sql(
                "INSERT INTO \"roles\" (\"name\",\"description\",\"created_at\",\"updated_at\") " +
                "VALUES ('Administrator','System administrator with full access.', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);");
        }
    }
}
