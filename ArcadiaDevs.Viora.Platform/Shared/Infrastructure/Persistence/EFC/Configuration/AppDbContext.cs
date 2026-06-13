using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregate;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Entities; // Import the new entity namespace
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
    ///     Gets the agronomic statistics set.
    /// </summary>
    public DbSet<AgronomicStatistic> AgronomicStatistics => Set<AgronomicStatistic>();

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