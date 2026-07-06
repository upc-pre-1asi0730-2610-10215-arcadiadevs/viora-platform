using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Billing.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Profile.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Application database context shared by the bounded contexts.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    /// <summary>
    ///     Gets the plots set.
    /// </summary>
    public DbSet<Plot> Plots => Set<Plot>();

    /// <summary>
    ///     Gets the AgroMonitoring integrations set.
    /// </summary>
    public DbSet<AgroMonitoringPlotIntegration> AgroMonitoringPlotIntegrations => Set<AgroMonitoringPlotIntegration>();

    /// <summary>
    ///     Gets the dynamic nutrition plans set.
    /// </summary>
    public DbSet<DynamicNutritionPlan> DynamicNutritionPlans => Set<DynamicNutritionPlan>();

    /// <summary>
    ///     Gets the users set.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    ///     Gets the roles set.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        // SHARED-011: the interceptors are no longer registered here.
        // The composition root (Program.cs) registers them via the
        // AddDbContext<AppDbContext> lambda in the locked order:
        //   1) AuditableEntityInterceptor FIRST (audit timestamps set
        //      before the post-commit dispatcher reads the entity)
        //   2) PostCommitDomainEventDispatcher LAST (post-commit dispatch)
        // This keeps DbContext configuration centralized in the
        // composition root and lets both interceptors be DI-injected
        // (the previous in-method construction could not consume
        // services from the host's DI container).
        base.OnConfiguring(builder);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // The model is intentionally schema-agnostic: no default schema is baked
        // into the model (and therefore into migrations/snapshot). The target
        // schema is selected per-environment through the connection's `Search Path`
        // (= DATABASE_SCHEMA), so unqualified tables are created and queried in the
        // right schema on shared hosts (e.g. Filess.io, where the user has no rights
        // on "public") and in "public" for local development. Baking the env-driven
        // schema via HasDefaultSchema made the runtime model diverge from the
        // migration snapshot whenever DATABASE_SCHEMA != "public", which tripped
        // EF Core's PendingModelChangesWarning and aborted startup.
        //
        // SHARED-014: per-BC Apply<BC>Configuration extension methods replace
        // ApplyConfigurationsFromAssembly so each bounded context owns its own
        // EF Core mapping. The call order is alphabetical by BC and matches the
        // order used to generate the migration snapshot.
        builder.ApplyAgronomicConfiguration();
        builder.ApplyBillingConfiguration();
        builder.ApplyIamConfiguration();
        builder.ApplyInterventionConfiguration();
        builder.ApplyProfileConfiguration();
        builder.ApplySurveillanceConfiguration();
        builder.UseSnakeCaseNamingConvention();
    }
}
