using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Covers REQ-1 / design Decision 1 (obs #155, #156): the
///     <c>AlignRolesToOsTaxonomy</c> migration converges the <c>roles</c>
///     table to os-viora-platform's exact 2-role model on a real Postgres
///     instance.
///     <para>
///         Boots a fresh <see cref="PostgreSqlBuilder"/> (same pattern as
///         <see cref="MigrationSmokeTest"/>), migrates up to the migration
///         immediately BEFORE <c>AlignRolesToOsTaxonomy</c> via
///         <c>IMigrator</c>, seeds the pre-migration 3-role state directly
///         via raw SQL (the seeder already emits the post-migration
///         2-role names, so it cannot be reused to build this fixture),
///         then applies (and rolls back) <c>AlignRolesToOsTaxonomy</c> to
///         assert the forward + rollback SQL behave per the migration's
///         documented contract.
///     </para>
///     <para>
///         Requires Docker (Testcontainers.PostgreSql) — skipped by the
///         repo's Docker-free default filter
///         (<c>--filter "Database!=Postgres"</c>).
///     </para>
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "Postgres")]
public class RoleMigrationTests : IAsyncLifetime
{
    private const string MigrationUnderTest = "AlignRolesToOsTaxonomy";

    private PostgreSqlContainer? _container;
    private DbContextOptions<AppDbContext>? _options;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        var databaseName = $"viora_role_migration_{Guid.NewGuid():N}";
        _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase(databaseName)
            .WithUsername("viora")
            .WithPassword("viora")
            .WithPortBinding(0, 5432)
            .Build();
        await _container.StartAsync();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    ///     Up() renames the 2 surviving roles and deletes the
    ///     <c>Administrator</c> row, cascading the delete to any
    ///     <c>user_roles</c> rows that referenced it
    ///     (<c>f_k_user_roles_roles_roles_id</c> is <c>onDelete: Cascade</c>,
    ///     20260625052534_AddRoles).
    /// </summary>
    [Fact]
    public async Task Up_RenamesSurvivorsAndDeletesAdministrator_CascadingUserRoles()
    {
        // GIVEN all migrations up to (excluding) AlignRolesToOsTaxonomy are
        // applied, then the pre-migration 3-role state + a user assigned to
        // Administrator are seeded directly (the seeder already emits the
        // post-migration names, so it cannot be reused to build this
        // fixture).
        await using var context = new AppDbContext(_options!);
        var migrator = context.Database.GetService<IMigrator>();
        var migrationBeforeUnderTest = MigrationBefore(context);
        await migrator.MigrateAsync(migrationBeforeUnderTest);

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "roles" ("name", "description") VALUES
                ('OliveProducer', 'Olive oil producer with plot management access.'),
                ('PhytosanitarySpecialist', 'Specialist in plant health and pest surveillance.'),
                ('Administrator', 'System administrator with full access.');
            INSERT INTO "users" ("username", "password_hash") VALUES ('admin-fixture', 'irrelevant-hash');
            INSERT INTO "user_roles" ("roles_id", "users_id")
                SELECT r."id", u."id" FROM "roles" r, "users" u
                WHERE r."name" = 'Administrator' AND u."username" = 'admin-fixture';
            """);

        // WHEN AlignRolesToOsTaxonomy's Up() is applied.
        await migrator.MigrateAsync(MigrationUnderTest);

        // THEN exactly 2 roles remain, with the OS-aligned names.
        await using var verifyContext = new AppDbContext(_options!);
        var roleNames = await verifyContext.Set<Role>()
            .Select(r => r.Name)
            .ToListAsync();
        Assert.Equal(2, roleNames.Count);
        Assert.Contains("Grower", roleNames);
        Assert.Contains("Specialist", roleNames);
        Assert.DoesNotContain("Administrator", roleNames);

        // AND the user_roles row that referenced Administrator was
        // cascade-deleted (no orphaned FK row survives).
        var remainingUserRoles = await CountAsync(verifyContext, "user_roles");
        Assert.Equal(0, remainingUserRoles);
    }

    /// <summary>
    ///     Down() reverses the 2 renames and re-inserts the
    ///     <c>Administrator</c> role row, but — documented, accepted data
    ///     loss — does NOT restore a <c>user_roles</c> link that was
    ///     cascade-deleted by the forward migration.
    /// </summary>
    [Fact]
    public async Task Down_RestoresAdministratorRoleRow_WithoutRestoringUserRoleLinks()
    {
        // GIVEN all migrations applied (roles are now Grower/Specialist,
        // Administrator is gone) and a user assigned to Grower.
        await using var context = new AppDbContext(_options!);
        await context.Database.MigrateAsync();

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO "users" ("username", "password_hash") VALUES ('grower-fixture', 'irrelevant-hash');
            INSERT INTO "user_roles" ("roles_id", "users_id")
                SELECT r."id", u."id" FROM "roles" r, "users" u
                WHERE r."name" = 'Grower' AND u."username" = 'grower-fixture';
            """);

        // WHEN AlignRolesToOsTaxonomy is rolled back.
        var migrator = context.Database.GetService<IMigrator>();
        var migrationBeforeUnderTest = MigrationBefore(context);
        await migrator.MigrateAsync(migrationBeforeUnderTest);

        // THEN the Administrator role row is restored (with a fresh id),
        // alongside the 2 reverse-renamed survivors.
        await using var verifyContext = new AppDbContext(_options!);
        var roleNames = await verifyContext.Set<Role>()
            .Select(r => r.Name)
            .ToListAsync();
        Assert.Equal(3, roleNames.Count);
        Assert.Contains("OliveProducer", roleNames);
        Assert.Contains("PhytosanitarySpecialist", roleNames);
        Assert.Contains("Administrator", roleNames);

        // AND the previously-existing user_roles link (Grower ->
        // grower-fixture, cascade-deleted when the Grower row was renamed
        // back to OliveProducer) is NOT restored — accepted data loss,
        // documented on the migration's Down().
        var remainingUserRoles = await CountAsync(verifyContext, "user_roles");
        Assert.Equal(0, remainingUserRoles);
    }

    /// <summary>
    ///     Returns the name of the migration immediately before
    ///     <see cref="MigrationUnderTest"/> in the ordered migration list,
    ///     used as the <c>IMigrator</c> target to migrate up to (but not
    ///     including) <c>AlignRolesToOsTaxonomy</c>.
    /// </summary>
    private static string MigrationBefore(AppDbContext context)
    {
        var ordered = context.Database.GetMigrations().ToList();
        var index = ordered.FindIndex(m => m.Contains(MigrationUnderTest, StringComparison.Ordinal));
        Assert.True(index > 0, $"Expected '{MigrationUnderTest}' to exist with a predecessor migration.");
        return ordered[index - 1];
    }

    private static async Task<long> CountAsync(AppDbContext context, string table)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
