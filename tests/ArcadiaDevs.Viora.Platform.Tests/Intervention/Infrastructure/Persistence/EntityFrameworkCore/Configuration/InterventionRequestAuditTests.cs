using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

/// <summary>
///     Integration-shaped unit tests proving <see cref="InterventionRequest"/> — via its
///     real <see cref="InterventionRequestConfiguration"/> mapping — is correctly stamped
///     by the shared <see cref="AuditableEntityInterceptor"/> on insert and update. Mirrors
///     the generic-fixture coverage in
///     <c>Shared/Infrastructure/Persistence/EntityFrameworkCore/Interceptors/AuditableEntityInterceptorTests.cs</c>,
///     but exercises the actual production aggregate + entity configuration instead of a
///     fake fixture entity, closing the gap the specialist-dashboard-parity feature
///     introduced when <c>CreatedAt</c>/<c>UpdatedAt</c> were added to
///     <see cref="InterventionRequest"/> (2026-07-05) for the incoming-request inbox.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class InterventionRequestAuditTests
{
    /// <summary>
    ///     Minimal DbContext registering ONLY the InterventionRequest configuration —
    ///     enough to exercise the interceptor against the real entity without pulling in
    ///     the full <c>AppDbContext</c> model (which would require every other BC's
    ///     configuration to also be satisfiable against the InMemory provider).
    /// </summary>
    private sealed class FixtureDbContext(DbContextOptions<FixtureDbContext> options) : DbContext(options)
    {
        public DbSet<InterventionRequest> InterventionRequests => Set<InterventionRequest>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new InterventionRequestConfiguration());
        }
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
    ///     GIVEN a brand-new InterventionRequest added to the context
    ///     WHEN SaveChangesAsync persists it for the first time
    ///     THEN both CreatedAt and UpdatedAt are stamped from the injected clock.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnInsert_SetsCreatedAtAndUpdatedAt()
    {
        // GIVEN a clock frozen at a known instant
        var seed = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);
        var clock = new FakeClock(seed);
        await using var context = CreateContext(clock, Guid.NewGuid().ToString());

        var request = new InterventionRequest(
            growerId: 1, plotId: 1, specialistId: 1, alertId: null,
            reason: "Pest sighting", message: "Please assess my plot");
        context.InterventionRequests.Add(request);

        // WHEN the request is inserted
        await context.SaveChangesAsync();

        // THEN both timestamps are stamped from the clock
        var expected = new DateTimeOffset(seed, TimeSpan.Zero);
        Assert.Equal(expected, request.CreatedAt);
        Assert.Equal(expected, request.UpdatedAt);
    }

    /// <summary>
    ///     GIVEN a persisted InterventionRequest and a clock that advances to a new instant
    ///     WHEN the request is mutated (e.g. via Verify) and saved again
    ///     THEN UpdatedAt moves to the new instant while CreatedAt stays at the original
    ///     insert-time value — the same <c>CreatedAt ??= now</c> semantics proven generically
    ///     for <see cref="Shared.Domain.Model.Entities.IAuditableEntity"/>, now confirmed for
    ///     the real InterventionRequest aggregate specifically.
    /// </summary>
    [Fact]
    public async Task SaveChangesAsync_OnVerifyUpdate_ChangesUpdatedAt_ButKeepsCreatedAtStable()
    {
        // GIVEN a request inserted at a first clock instant
        var firstInstant = new DateTime(2026, 7, 5, 9, 0, 0, DateTimeKind.Utc);
        var secondInstant = new DateTime(2026, 7, 6, 14, 30, 0, DateTimeKind.Utc);
        var clock = new FakeClock(firstInstant);
        await using var context = CreateContext(clock, Guid.NewGuid().ToString());

        var request = new InterventionRequest(
            growerId: 1, plotId: 1, specialistId: 1, alertId: null,
            reason: "Pest sighting", message: "Please assess my plot");
        context.InterventionRequests.Add(request);
        await context.SaveChangesAsync();

        var createdAtAfterInsert = request.CreatedAt;

        // WHEN the clock advances and the specialist verifies the request (a real domain
        // mutation, not a synthetic field change), then it is saved again
        clock.Set(secondInstant);
        request.Verify();
        await context.SaveChangesAsync();

        // THEN CreatedAt is unchanged from the original insert-time value...
        var expectedCreatedAt = new DateTimeOffset(firstInstant, TimeSpan.Zero);
        Assert.Equal(expectedCreatedAt, createdAtAfterInsert);
        Assert.Equal(expectedCreatedAt, request.CreatedAt);

        // ...while UpdatedAt moved to the second instant.
        var expectedUpdatedAt = new DateTimeOffset(secondInstant, TimeSpan.Zero);
        Assert.Equal(expectedUpdatedAt, request.UpdatedAt);
    }
}
