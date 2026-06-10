using ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Extension methods that register all Agronomic bounded-context entity configurations
///     into the <see cref="ModelBuilder"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies all Agronomic entity type configurations to the model builder.
    /// </summary>
    /// <param name="builder">The EF Core model builder.</param>
    public static void ApplyAgronomicConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new PlotConfiguration());
        builder.ApplyConfiguration(new IoTDeviceConfiguration());
    }
}