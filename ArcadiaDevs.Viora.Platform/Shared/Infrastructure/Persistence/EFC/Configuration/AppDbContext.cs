using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

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
        builder.ApplyConfigurationsFromAssembly(typeof(PlotConfiguration).Assembly);
        builder.UseSnakeCaseNamingConvention();
    }
}
