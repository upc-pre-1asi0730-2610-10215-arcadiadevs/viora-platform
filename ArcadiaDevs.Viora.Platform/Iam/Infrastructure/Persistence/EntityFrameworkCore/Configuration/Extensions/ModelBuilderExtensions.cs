using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration.Extensions;

/// <summary>
///     Model builder extensions for the IAM (Identity and Access Management) bounded context.
/// </summary>
/// <remarks>
///     Each <see cref="IEntityTypeConfiguration{TEntity}" /> in the IAM BC is applied
///     explicitly here so the BC owns its EF Core mapping. The
///     <see cref="AppDbContext" /> orchestrates the call order across BCs; the BC
///     itself owns which entity configurations are wired in.
/// </remarks>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Applies every IAM <see cref="IEntityTypeConfiguration{TEntity}" />
    ///     to the supplied <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    public static void ApplyIamConfiguration(this ModelBuilder builder)
    {
        builder.ApplyConfiguration(new UserConfiguration());
        builder.ApplyConfiguration(new RoleConfiguration());
    }
}
