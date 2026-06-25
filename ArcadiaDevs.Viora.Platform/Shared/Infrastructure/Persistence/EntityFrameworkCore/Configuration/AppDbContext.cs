using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
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

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.AddInterceptors(new AuditableEntityInterceptor());
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
        builder.ApplyConfigurationsFromAssembly(typeof(PlotConfiguration).Assembly);
        builder.UseSnakeCaseNamingConvention();
    }
}
