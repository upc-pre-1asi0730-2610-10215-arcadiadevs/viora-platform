using ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the Intervention bounded context.
/// </summary>
/// <remarks>
///     Each <see cref="IEntityTypeConfiguration{TEntity}" /> in the
///     Intervention BC is applied explicitly here so the BC owns its EF
///     Core mapping, mirroring Surveillance's <c>ModelBuilderExtensions</c>.
///     WU1 registered <c>SpecialistConfiguration</c>; WU3 adds
///     <c>InterventionRequestConfiguration</c>; WU4-WU8 extend this method
///     as their aggregates land (per-slice migrations, obs #269).
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies every Intervention <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     to the supplied <paramref name="builder" />.
    /// </summary>
    public static void ApplyInterventionConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new SpecialistConfiguration());
        builder.ApplyConfiguration(new InterventionRequestConfiguration());
    }
}
