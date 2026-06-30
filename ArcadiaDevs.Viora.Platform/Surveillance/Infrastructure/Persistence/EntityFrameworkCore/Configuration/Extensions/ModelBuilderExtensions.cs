using ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the Surveillance bounded context.
/// </summary>
/// <remarks>
///     Each <see cref="IEntityTypeConfiguration{TEntity}" /> in the Surveillance BC
///     is applied explicitly here so the BC owns its EF Core mapping. The
///     <see cref="AppDbContext" /> orchestrates the call order across BCs; the BC
///     itself owns which entity configurations are wired in.
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies every Surveillance <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     to the supplied <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplySurveillanceConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new AlertConfiguration());
        builder.ApplyConfiguration(new PestSightingReportConfiguration());
        builder.ApplyConfiguration(new SymptomDictionaryItemConfiguration());
    }
}
