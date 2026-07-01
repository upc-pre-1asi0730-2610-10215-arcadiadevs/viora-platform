using Testcontainers.PostgreSql;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     xUnit <see cref="IAsyncLifetime"/> fixture that owns a
///     <see cref="PostgreSqlContainer"/> for the lifetime of a test class
///     (one container per class). Subclasses get the resolved connection
///     string via <see cref="ConnectionString"/>.
/// </summary>
/// <remarks>
///     Pattern: <c>[Collection("Postgres")]</c> on the test class +
///     <c>IClassFixture&lt;TestcontainersFixture&lt;PostgresTestContainer&gt;&gt;</c>
///     — the <c>[Collection]</c> ensures only one container is started
///     per xUnit collection (so multiple test classes in the same
///     collection share the container). Standalone usage without
///     <c>[Collection]</c> starts a new container per class.
/// </remarks>
public class TestcontainersFixture<TContainer> : IAsyncLifetime
    where TContainer : IAsyncLifetime, new()
{
    public TContainer Container { get; } = new();

    public string ConnectionString => Container switch
    {
        PostgresTestContainer postgres => postgres.ConnectionString,
        _ => throw new InvalidOperationException(
            $"TestcontainersFixture does not know how to extract a connection string from {typeof(TContainer).FullName}.")
    };

    public async Task InitializeAsync() => await Container.InitializeAsync();
    public async Task DisposeAsync() => await Container.DisposeAsync();
}
