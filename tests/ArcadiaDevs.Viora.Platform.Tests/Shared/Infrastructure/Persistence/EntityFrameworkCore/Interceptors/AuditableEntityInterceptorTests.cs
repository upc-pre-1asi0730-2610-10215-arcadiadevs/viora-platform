using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;

/// <summary>
///     Unit tests for <see cref="AuditableEntityInterceptor"/>, which stamps
///     <see cref="IAuditableEntity.CreatedAt"/>/<see cref="IAuditableEntity.UpdatedAt"/>
///     from the injected <see cref="IClock"/> on every <c>SaveChanges</c>/<c>SaveChangesAsync</c>.
///     Uses EF Core's InMemory provider with a minimal fixture DbContext/entity.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class AuditableEntityInterceptorTests
{
    /// <summary>
    ///     Minimal <see cref="IAuditableEntity"/> fixture for exercising the interceptor.
    /// </summary>
    private sealed class FakeAuditableEntity : IAuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    /// <summary>
    ///     Minimal DbContext fixture wired with the interceptor under test.
    /// </summary>
    private sealed class FixtureDbContext(DbContextOptions<FixtureDbContext> options) : DbContext(options)
    {
        public DbSet<FakeAuditableEntity> Entities => Set<FakeAuditableEntity>();
    }

    private static FixtureDbContext CreateContext(IClock clock, string databaseName)
    {
        var options = new DbContextOptionsBuilder<FixtureDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new AuditableEntityInterceptor(clock))
            .Options;
        return new FixtureDbContext(options);
    }

    /// <summary>
    ///     GIVEN a new entity added to the context
    ///     WHEN SaveChangesAsync is called for the first time (EntityState.Added)
    ///     THEN CreatedAt and UpdatedAt are both stamped from the injected FakeClock.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnInsert_SetsCreatedAtAndUpdatedAt_FromInjectedClock()
    {
        // GIVEN a clock frozen at a known instant
        var seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var clock = new FakeClock(seed);
        await using var context = CreateContext(clock, Guid.NewGuid().ToString());

        var entity = new FakeAuditableEntity { Name = "initial" };
        context.Entities.Add(entity);

        // WHEN the entity is inserted
        await context.SaveChangesAsync();

        // THEN both timestamps are derived from the clock, not left null/default
        var expected = new DateTimeOffset(seed, TimeSpan.Zero);
        Assert.Equal(expected, entity.CreatedAt);
        Assert.Equal(expected, entity.UpdatedAt);
    }

    /// <summary>
    ///     GIVEN an entity already persisted (and still tracked) via a prior SaveChanges
    ///     WHEN the clock advances to a DIFFERENT instant, the entity is modified, and
    ///     SaveChangesAsync is called again (EntityState.Modified)
    ///     THEN UpdatedAt reflects the new clock instant while CreatedAt stays at the
    ///     original insert-time instant (the <c>CreatedAt ??= now</c> semantics: only
    ///     EntityState.Added ever touches CreatedAt).
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnUpdate_ChangesUpdatedAt_ButKeepsCreatedAtStable()
    {
        // GIVEN an entity inserted at a first clock instant
        var firstInstant = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var secondInstant = new DateTime(2026, 1, 5, 8, 30, 0, DateTimeKind.Utc);
        var clock = new FakeClock(firstInstant);
        await using var context = CreateContext(clock, Guid.NewGuid().ToString());

        var entity = new FakeAuditableEntity { Name = "initial" };
        context.Entities.Add(entity);
        await context.SaveChangesAsync();

        var createdAtAfterInsert = entity.CreatedAt;
        var updatedAtAfterInsert = entity.UpdatedAt;

        // WHEN the SAME FakeClock instance advances to a distinct instant and the
        // already-tracked entity is modified and saved again
        clock.Set(secondInstant);
        entity.Name = "changed";
        await context.SaveChangesAsync();

        // THEN CreatedAt is unchanged from the original insert-time value...
        var expectedCreatedAt = new DateTimeOffset(firstInstant, TimeSpan.Zero);
        Assert.Equal(expectedCreatedAt, createdAtAfterInsert);
        Assert.Equal(expectedCreatedAt, entity.CreatedAt);

        // ...while UpdatedAt moved to the second, distinct clock instant — proving both
        // values are actually clock-derived (not merely non-null) and that CreatedAt and
        // UpdatedAt are independently stamped.
        var expectedUpdatedAt = new DateTimeOffset(secondInstant, TimeSpan.Zero);
        Assert.NotEqual(updatedAtAfterInsert, entity.UpdatedAt);
        Assert.Equal(expectedUpdatedAt, entity.UpdatedAt);
    }
}
