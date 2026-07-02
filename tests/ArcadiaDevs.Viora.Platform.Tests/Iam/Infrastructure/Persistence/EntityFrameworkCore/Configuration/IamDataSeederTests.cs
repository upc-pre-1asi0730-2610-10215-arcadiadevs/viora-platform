using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Covers REQ-1 (role taxonomy alignment, obs #156): the seeder MUST
///     seed exactly the 2 OS-aligned roles (<c>Grower</c>, <c>Specialist</c>)
///     and MUST NOT seed an <c>Administrator</c> row (removed entirely, not
///     renamed — design Decision 1, obs #155).
/// </summary>
public class IamDataSeederTests
{
    [Fact]
    public async Task SeedAsync_NewNames_SeedsExpectedRoles()
    {
        // GIVEN a fresh in-memory database with no roles.
        await using var context = TestHarness.InMemoryRepositories.NewInMemoryDbContext();

        // WHEN the seeder runs.
        await IamDataSeeder.SeedAsync(context);

        // THEN exactly 2 roles exist — Grower and Specialist — and no
        // Administrator row (removed entirely per design Decision 1).
        var roleNames = await context.Set<Role>().Select(r => r.Name).ToListAsync();

        Assert.Equal(2, roleNames.Count);
        Assert.Contains("Grower", roleNames);
        Assert.Contains("Specialist", roleNames);
        Assert.DoesNotContain("Administrator", roleNames);
        Assert.DoesNotContain("OliveProducer", roleNames);
        Assert.DoesNotContain("PhytosanitarySpecialist", roleNames);
    }

    [Fact]
    public async Task SeedAsync_CalledTwice_IsIdempotent()
    {
        // GIVEN a fresh in-memory database.
        await using var context = TestHarness.InMemoryRepositories.NewInMemoryDbContext();

        // WHEN the seeder runs twice (simulates a second app boot against
        // an already-seeded database).
        await IamDataSeeder.SeedAsync(context);
        await IamDataSeeder.SeedAsync(context);

        // THEN no duplicate rows are created.
        var roleNames = await context.Set<Role>().Select(r => r.Name).ToListAsync();
        Assert.Equal(2, roleNames.Count);
    }
}
