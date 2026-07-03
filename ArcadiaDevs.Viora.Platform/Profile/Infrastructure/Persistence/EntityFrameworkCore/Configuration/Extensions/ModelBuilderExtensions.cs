using ArcadiaDevs.Viora.Platform.Profile.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Profile.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the Profile bounded context.
/// </summary>
/// <remarks>
///     Each <see cref="IEntityTypeConfiguration{TEntity}" /> in the Profile BC is
///     applied explicitly here so the BC owns its EF Core mapping. The
///     <see cref="AppDbContext" /> orchestrates the call order across BCs; the BC
///     itself owns which entity configurations are wired in.
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies every Profile <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     to the supplied <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyProfileConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new ProfileConfiguration());
    }
}
