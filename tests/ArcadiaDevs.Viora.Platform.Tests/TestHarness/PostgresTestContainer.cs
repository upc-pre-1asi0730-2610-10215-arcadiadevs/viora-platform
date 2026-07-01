using Testcontainers.PostgreSql;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Concrete <see cref="Testcontainers.PostgreSql"/> configuration for the
///     F1a-F6b integration tests. One container per test class (via
///     <see cref="TestcontainersFixture{T}"/>); each container gets a unique
///     database name <c>viora_test_{guid}</c> so parallel test classes do not
///     collide on schema. The container is bound to a dynamic port (port 0)
///     so multiple containers can coexist on the same host without conflict.
/// </summary>
/// <remarks>
///     Image: <c>postgres:16-alpine</c> (matches the project's
///     <c>appsettings.json</c> PostgreSQL major version; alpine for fast
///     pull-and-start). Username: <c>viora</c>. Password: <c>viora</c>.
///     Database name: <c>viora_test_{guid}</c> (unique per container to
///     avoid shared-state across test classes).
/// </remarks>
public sealed class PostgresTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlBuilder _builder;
    private PostgreSqlContainer? _container;

    /// <summary>
    ///     The connection string the test fixture will point the
    ///     <c>ArcadiaDevs.Viora.Platform</c> host at. Resolves to
    ///     <c>Host=127.0.0.1;Port={random};Username=viora;Password=viora;Database=viora_test_{guid}</c>
    ///     once the container is started.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString()
        ?? throw new InvalidOperationException(
            "PostgresTestContainer has not been started yet. Call InitializeAsync() first.");

    public PostgresTestContainer()
    {
        var databaseName = $"viora_test_{Guid.NewGuid():N}";
        _builder = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase(databaseName)
            .WithUsername("viora")
            .WithPassword("viora")
            .WithPortBinding(0, 5432);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _container = _builder.Build();
        await _container.StartAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
