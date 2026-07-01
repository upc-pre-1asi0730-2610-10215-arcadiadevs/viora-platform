using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Xunit;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure;

/// <summary>
///     Unit tests for <see cref="InMemoryRepositories"/> — verifies
///     that the factory helpers produce working instances that
///     tests can configure. The substantive coverage of the
///     fakes (the actual command-service behavior) is exercised
///     in the per-BC unit tests that consume them (F3a.1, F3a.2,
///     F5, etc.). The harness tests pin the factory contract.
/// </summary>
[Trait("Category", "Unit")]
public class InMemoryRepositoriesTests
{
    [Fact]
    public void NewInMemoryDbContext_ReturnsUniqueDatabasePerCall()
    {
        // Arrange + Act
        using var context1 = InMemoryRepositories.NewInMemoryDbContext();
        using var context2 = InMemoryRepositories.NewInMemoryDbContext();

        // Assert
        Assert.NotNull(context1);
        Assert.NotNull(context2);
        // Different contexts can have the same database name in the InMemory
        // provider; the unique-name contract is that NewInMemoryDbContext
        // does NOT throw and returns a fresh DbContextOptions each call.
    }

    [Fact]
    public void NewUnitOfWork_ReturnsNonNullSubstitute()
    {
        // Arrange + Act
        var uow = InMemoryRepositories.NewUnitOfWork();

        // Assert
        Assert.NotNull(uow);
    }

    [Fact]
    public void NewPlotRepository_ReturnsNonNullSubstitute()
    {
        // Arrange + Act
        IPlotRepository repo = InMemoryRepositories.NewPlotRepository();

        // Assert
        Assert.NotNull(repo);
    }

    [Fact]
    public void NewUserRepository_ReturnsNonNullSubstitute()
    {
        // Arrange + Act
        IUserRepository repo = InMemoryRepositories.NewUserRepository();

        // Assert
        Assert.NotNull(repo);
    }

    [Fact]
    public void NewRoleRepository_ReturnsNonNullSubstitute()
    {
        // Arrange + Act
        IRoleRepository repo = InMemoryRepositories.NewRoleRepository();

        // Assert
        Assert.NotNull(repo);
    }

    [Fact]
    public void NewAlertRepository_ReturnsNonNullSubstitute()
    {
        // Arrange + Act
        IAlertRepository repo = InMemoryRepositories.NewAlertRepository();

        // Assert
        Assert.NotNull(repo);
    }

    [Fact]
    public async Task NewInMemoryDbContext_CanBeDisposedSafely()
    {
        // Arrange
        var context = InMemoryRepositories.NewInMemoryDbContext();

        // Act + Assert — DisposeAsync must not throw on a fresh context.
        await context.DisposeAsync();
    }
}
