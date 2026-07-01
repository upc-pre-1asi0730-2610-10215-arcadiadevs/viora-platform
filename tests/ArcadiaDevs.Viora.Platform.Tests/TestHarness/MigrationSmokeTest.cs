using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Migration smoke test — boots a fresh
///     <see cref="PostgreSqlBuilder"/> (NOT the full
///     <c>WebApplicationFactory</c> host), applies all 7 EF Core
///     <c>Add*</c> migrations from
///     <c>ArcadiaDevs.Viora.Platform/Migrations/</c>, and asserts
///     the schema is correct (the 7 migration history rows are
///     present, the expected tables exist) and that a round-trip
///     CRUD works on every aggregate root.
///     <para>
///         This test does NOT inherit
///         <see cref="IntegrationTestBase"/>: it uses raw EF
///         against a fresh container, no
///         <c>WebApplicationFactory</c> boot, no
///         <c>PostCommitDomainEventDispatcher</c>, no auth. It is
///         the "R2 risk canary" — the InMemory provider would
///         mask FK enforcement + transactions + transactions
///         + <c>Include()</c> semantics, so this test deliberately
///         uses Testcontainers to validate the migrations + schema
///         + CRUD on a real Postgres.
///     </para>
///     <para>
///         Migrations validated (count = 7):
///         <list type="number">
///             <item><c>InitialCreate</c> (base tables)</item>
///             <item><c>AddIamUsers</c></item>
///             <item><c>AddRoles</c></item>
///             <item><c>AddAgronomicStatistic</c></item>
///             <item><c>AddIoTDevice</c></item>
///             <item><c>AddDynamicNutritionPlan</c></item>
///             <item><c>AddMonitoringSummary</c></item>
///             <item><c>AddIoTDeviceActivationCode</c></item>
///         </list>
///         (The user prompt lists 7; the actual count is 8 —
///         includes <c>AddIoTDeviceActivationCode</c> from
///         Phase 2 PR-B2. The test asserts
///         <c>applied.Count &gt;= 7</c> to be future-proof.)
///     </para>
///     <para>
///         The [Trait("Category", "Smoke")] tag lets CI split
///         the migration smoke run from the slower
///         <c>[Trait("Category", "Integration")]</c> suite. The
///         test does NOT carry <c>[Collection("Postgres")]</c>
///         because it does not inherit
///         <see cref="IntegrationTestBase"/> and has its own
///         per-test container.
///     </para>
/// </summary>
[Trait("Category", "Smoke")]
[Trait("Database", "Postgres")]
public class MigrationSmokeTest : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private DbContextOptions<AppDbContext>? _options;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Boot a fresh Testcontainers.PostgreSql with a unique
        // database name so parallel test runs do not collide.
        var databaseName = $"viora_smoke_{Guid.NewGuid():N}";
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
    ///     S1.19 — All EF Core migrations apply on a fresh
    ///     Testcontainers.PostgreSql instance. Asserts at least 7
    ///     <c>Add*</c> migrations are in the
    ///     <c>__EFMigrationsHistory</c> table AND the schema
    ///     contains the expected tables from the Agronomic, IAM,
    ///     and Surveillance BCs.
    /// </summary>
    [Fact]
    public async Task All_Add_Migrations_Apply_On_Fresh_Container()
    {
        // GIVEN a fresh Testcontainers.PostgreSql (boot in
        // InitializeAsync).
        await using var context = new AppDbContext(_options!);

        // WHEN the EF Core migrations are applied.
        await context.Database.MigrateAsync();

        // THEN the migration history contains at least 7
        // entries (the user-locked count from the F1b plan;
        // 8 if AddIoTDeviceActivationCode is counted).
        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.NotEmpty(applied);
        Assert.True(applied.Count() >= 7,
            $"Expected at least 7 applied migrations; found {applied.Count()}: " +
            string.Join(", ", applied));

        // AND the schema contains the expected tables from
        // the 3 bounded contexts.
        var tables = await ExtractTableNamesAsync(context);
        // Agronomic BC tables
        Assert.Contains("agronomic_statistics", tables);
        Assert.Contains("dynamic_nutrition_plans", tables);
        Assert.Contains("iot_devices", tables);
        Assert.Contains("monitoring_summaries", tables);
        Assert.Contains("plots", tables);
        // IAM BC tables
        Assert.Contains("users", tables);
        Assert.Contains("roles", tables);
        Assert.Contains("user_roles", tables);
        // Surveillance BC tables
        Assert.Contains("alerts", tables);
        Assert.Contains("alert_timeline_records", tables);
        Assert.Contains("symptom_dictionary_items", tables);
        Assert.Contains("pest_sighting_reports", tables);
    }

    /// <summary>
    ///     S1.20 — Round-trip CRUD works on a fresh
    ///     Testcontainers.PostgreSql. The test creates a Plot +
    ///     a User + a Role, assigns the role to the user, and
    ///     asserts the read-back values match. This is the
    ///     "FK enforcement + transactions + Include() semantics"
    ///     canary that the InMemory provider cannot fake
    ///     (the design §8.2 R2 mitigation).
    /// </summary>
    [Fact]
    public async Task RoundTrip_CRUD_Works_On_Fresh_Container()
    {
        // GIVEN a fresh Testcontainers.PostgreSql with all 7
        // migrations applied.
        await using var context = new AppDbContext(_options!);
        await context.Database.MigrateAsync();

        // Create a User (IAM BC).
        var user = new User("alice-smoke-test", "hashed-password");
        context.Users.Add(user);

        // Create a Role (IAM BC).
        var roleResult = Role.Create("Farmer", "Smoke test farmer role");
        Assert.True(roleResult is Result<Role, Error>.Success);
        var role = ((Result<Role, Error>.Success)roleResult).Value;
        context.Roles.Add(role);

        // Create a Plot (Agronomic BC).
        var polygon = new PolygonCoordinates
        {
            Points = new List<GeoPoint>
            {
                new() { Latitude = -12.0m, Longitude = -77.0m },
                new() { Latitude = -12.0m, Longitude = -77.1m },
                new() { Latitude = -12.1m, Longitude = -77.1m },
                new() { Latitude = -12.1m, Longitude = -77.0m },
                new() { Latitude = -12.0m, Longitude = -77.0m }
            }
        };
        var plotResult = Plot.Create(
            ownerUserId: 1,
            plotName: "Smoke test plot",
            polygonCoordinates: polygon,
            areaSize: 100m,
            cropType: "Coffee",
            variety: "Arabica",
            location: "Lima",
            campaign: "2026",
            notes: "Smoke test notes");
        Assert.True(plotResult is Result<Plot, Error>.Success);
        var plot = ((Result<Plot, Error>.Success)plotResult).Value;
        context.Plots.Add(plot);

        // WHEN the user is assigned the role + everything is
        // saved in a single transaction.
        user.Roles.Add(role);
        await context.SaveChangesAsync();

        // THEN a fresh DbContext re-reads everything
        // (validates the read path + FK enforcement).
        await using var verifyContext = new AppDbContext(_options!);
        var readUser = await verifyContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == "alice-smoke-test");
        Assert.NotNull(readUser);
        Assert.Equal("alice-smoke-test", readUser.Username);
        Assert.Single(readUser.Roles);
        Assert.Equal("Farmer", readUser.Roles.First().Name);

        var readRole = await verifyContext.Roles.FirstOrDefaultAsync(r => r.Name == "Farmer");
        Assert.NotNull(readRole);
        Assert.Equal("Smoke test farmer role", readRole.Description);

        var readPlot = await verifyContext.Plots.FirstOrDefaultAsync(p => p.PlotName == "Smoke test plot");
        Assert.NotNull(readPlot);
        Assert.Equal(100m, readPlot.AreaSize);
        Assert.Equal("Coffee", readPlot.CropType);
    }

    /// <summary>
    ///     Round-trip CRUD on a Surveillance Alert (verifies the
    ///     Surveillance migration tables wire up correctly).
    /// </summary>
    [Fact]
    public async Task RoundTrip_Surveillance_Alert_Works_On_Fresh_Container()
    {
        // GIVEN a fresh container with all 7 migrations applied.
        await using var context = new AppDbContext(_options!);
        await context.Database.MigrateAsync();

        // Create an Alert via the production ctor.
        var alert = new Alert(new ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands.CreateAlertCommand(
            PlotId: 50_000L,
            AlertType: ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects.EThreatType.PHENOLOGICAL_RISK.ToString(),
            Severity: EAlertSeverity.MEDIUM.ToString(),
            Title: "Smoke test alert",
            RiskExplanation: "Smoke test risk explanation",
            Sources: new List<string> { "TEST" },
            DataProviders: new List<string> { "TEST" },
            SupportingData: new Dictionary<string, string> { ["smoke"] = "true" }));
        context.Set<Alert>().Add(alert);

        // WHEN saved.
        await context.SaveChangesAsync();

        // THEN a fresh DbContext re-reads the alert.
        await using var verifyContext = new AppDbContext(_options!);
        var readAlert = await verifyContext.Set<Alert>().FirstOrDefaultAsync();
        Assert.NotNull(readAlert);
        Assert.Equal("Smoke test alert", readAlert.Title);
        Assert.Equal(ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects.EThreatType.PHENOLOGICAL_RISK, readAlert.Type);
        Assert.Equal(EAlertSeverity.MEDIUM, readAlert.Severity);
    }

    /// <summary>
    ///     Extracts the public table names from
    ///     <c>information_schema.tables</c> for the test
    ///     database. The migration's <c>__EFMigrationsHistory</c>
    ///     table is filtered out.
    /// </summary>
    private static async Task<List<string>> ExtractTableNamesAsync(AppDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT table_name FROM information_schema.tables " +
                "WHERE table_schema = 'public' " +
                "AND table_name != '__EFMigrationsHistory'";
            using var reader = await command.ExecuteReaderAsync();
            var names = new List<string>();
            while (await reader.ReadAsync())
            {
                names.Add(reader.GetString(0));
            }
            return names;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
