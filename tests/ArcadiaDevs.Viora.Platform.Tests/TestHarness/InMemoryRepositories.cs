using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Static factory helpers that produce a fresh set of
///     in-memory <see cref="DbContext"/> + NSubstitute-backed
///     repository fakes for fast command-service / query-service
///     unit tests. The factory is the canonical test infrastructure
///     for tests that do NOT need the full Testcontainers +
///     WebApplicationFactory harness (per design §1.7 — InMemory
///     reserved for tests that don't exercise EF).
/// </summary>
/// <remarks>
///     <para>
///         Each call to <see cref="NewInMemoryDbContext"/> returns a
///         new <see cref="DbContext"/> with a unique
///         <see cref="Database"/> name (via <c>Guid.NewGuid()</c>),
///         so test cases are isolated. Tests that need to share a
///         context can pass a fixed name.
///     </para>
///     <para>
///         The repository helpers return
///         <c>NSubstitute.For&lt;T&gt;()</c> substitutes. Tests that
///         need to assert the repository was called use
///         <c>_repo.Received(1).Method(...)</c>; tests that need to
///         pre-load data use the
///         <see cref="SeedAsync{TEntity}"/> helper.
///     </para>
/// </remarks>
public static class InMemoryRepositories
{
    /// <summary>
    ///     Returns a new <see cref="AppDbContext"/> backed by the
    ///     EF Core InMemory provider with a unique database name.
    ///     Each test gets its own database; tests do not share state.
    /// </summary>
    public static AppDbContext NewInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"viora_test_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    ///     Adds <paramref name="entity"/> to the context and saves.
    ///     Returns the persisted entity (the same reference, with
    ///     any auto-generated id populated).
    /// </summary>
    public static async Task<TEntity> SeedAsync<TEntity>(DbContext context, TEntity entity)
        where TEntity : class
    {
        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    ///     Returns a substitute for <see cref="IPlotRepository"/>.
    ///     Tests stub <c>FindByIdAsync</c> /
    ///     <c>HasRelatedOperationalRecordsAsync</c> /
    ///     <c>AddAsync</c> / etc. on the returned substitute.
    /// </summary>
    public static IPlotRepository NewPlotRepository() => Substitute.For<IPlotRepository>();

    /// <summary>
    ///     Returns a substitute for <see cref="IUserRepository"/>.
    ///     Tests stub <c>ExistsByUsernameAsync</c> /
    ///     <c>FindByUsernameAsync</c> / etc. on the returned
    ///     substitute.
    /// </summary>
    public static IUserRepository NewUserRepository() => Substitute.For<IUserRepository>();

    /// <summary>
    ///     Returns a substitute for <see cref="IRoleRepository"/>.
    ///     Tests stub <c>ExistsByNameAsync</c> / etc. on the
    ///     returned substitute.
    /// </summary>
    public static IRoleRepository NewRoleRepository() => Substitute.For<IRoleRepository>();

    /// <summary>
    ///     Returns a substitute for <see cref="IUnitOfWork"/>.
    ///     The default substitute returns a completed
    ///     <c>Task.CompletedTask</c> from <c>CompleteAsync</c>.
    /// </summary>
    public static IUnitOfWork NewUnitOfWork() => Substitute.For<IUnitOfWork>();

    /// <summary>
    ///     Returns a substitute for <see cref="IAlertRepository"/>.
    ///     Tests stub <c>FindByIdAsync</c> / etc. on the returned
    ///     substitute.
    /// </summary>
    public static IAlertRepository NewAlertRepository() => Substitute.For<IAlertRepository>();
}
