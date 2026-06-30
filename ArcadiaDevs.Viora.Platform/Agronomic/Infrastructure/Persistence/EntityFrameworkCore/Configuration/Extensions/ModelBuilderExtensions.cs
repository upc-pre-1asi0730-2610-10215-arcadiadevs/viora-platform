using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the Agronomic bounded context.
/// </summary>
/// <remarks>
///     Each <see cref="IEntityTypeConfiguration{TEntity}" /> in the Agronomic BC is
///     applied explicitly here so the BC owns its EF Core mapping. The
///     <see cref="AppDbContext" /> orchestrates the call order across BCs; the BC
///     itself owns which entity configurations are wired in.
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies every Agronomic <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     to the supplied <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyAgronomicConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new PlotConfiguration());
        builder.ApplyConfiguration(new IoTDeviceConfiguration());
        builder.ApplyConfiguration(new AgronomicStatisticConfiguration());
        builder.ApplyConfiguration(new AgroMonitoringPlotIntegrationConfiguration());
        builder.ApplyConfiguration(new DynamicNutritionPlanConfiguration());
    }
}
