using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Seeds the first-class roles into the database on application startup.
///     Idempotent: checks existence before insert, does not duplicate on re-run.
/// </summary>
public static class IamDataSeeder
{
    /// <summary>
    ///     Seeds the roles table with the three first-class roles.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public static async Task SeedAsync(AppDbContext context)
    {
        await SeedRoleAsync(context, "OliveProducer", "Olive oil producer with plot management access.");
        await SeedRoleAsync(context, "PhytosanitarySpecialist", "Specialist in plant health and pest surveillance.");
        await SeedRoleAsync(context, "Administrator", "System administrator with full access.");
    }

    private static async Task SeedRoleAsync(AppDbContext context, string name, string description)
    {
        var exists = await context.Set<Role>().AnyAsync(r => r.Name == name);
        if (!exists)
        {
            var createResult = Role.Create(name, description);
            if (createResult is Result<Role, Error>.Success success)
            {
                await context.Set<Role>().AddAsync(success.Value);
                await context.SaveChangesAsync();
            }
        }
    }
}
