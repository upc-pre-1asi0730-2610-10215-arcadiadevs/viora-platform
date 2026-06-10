using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates.Plot;
using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.Configurations;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Interceptors;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;

/// <summary>
///     Application database context for the Learning Center Platform
/// </summary>
/// <param name="options">
///     The options for the database context
/// </param>
public class AppDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>
    ///     Gets or sets the plots DbSet.
    /// </summary>
    public DbSet<Plot> Plots => Set<Plot>();

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        // Apply audit timestamp interceptor for all IAuditableEntity implementations
        builder.AddInterceptors(new AuditableEntityInterceptor());
        base.OnConfiguring(builder);
    }

    /// <summary>
    ///     On creating the database model
    /// </summary>
    /// <remarks>
    ///     This method is used to create the database model for the application.
    /// </remarks>
    /// <param name="builder">
    ///     The model builder for the database context
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // General Naming Convention for the database objects
        builder.UseSnakeCaseNamingConvention();

        // Apply entity type configurations from the Agronomic assembly
        builder.ApplyConfigurationsFromAssembly(typeof(PlotEntityTypeConfiguration).Assembly);
    }
}