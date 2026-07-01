using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Abstract base class for integration tests that need a
///     <see cref="WebApplicationFactory{TEntryPoint}"/> wired to a
///     Testcontainers.PostgreSql instance. Subclasses access the factory
///     via the <see cref="Factory"/> property and the resolved
///     <see cref="PostgresConnectionString"/> for direct DbContext use.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="Factory"/> is configured to use the test
///         configuration file <c>appsettings.Test.json</c> (Db provider
///         forced to <c>Postgres</c> + a stable JWT secret), and the
///         <see cref="PostgresConnectionString"/> from the
///         <see cref="PostgresTestContainer"/> is injected via the
///         <see cref="IConfiguration"/> in-memory provider so the
///         production host can find the connection string without
///         touching its <c>appsettings.json</c>.
///     </para>
///     <para>
///         Subclasses can further customize the host via
///         <see cref="WebApplicationFactory{TEntryPoint}.WithWebHostBuilder"/>,
///         e.g. to override <see cref="Shared.Domain.IClock"/> with
///         <see cref="FakeClock"/> or to inject NSubstitute mocks for
///         outbound services.
///     </para>
/// </remarks>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly PostgresTestContainer _postgres = new();
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;
    protected string PostgresConnectionString => _postgres.ConnectionString;

    public virtual async Task InitializeAsync()
    {
        // Start the Postgres container first so the connection string
        // is available when the host configures its DbContext.
        await _postgres.InitializeAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    // Append the test-specific configuration last so it
                    // overrides any other source (in particular, the
                    // production Database:Provider and the Jwt:Secret).
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Database:Provider"] = "Postgres",
                        ["ConnectionStrings:AppDbContext"] = _postgres.ConnectionString,
                        ["Jwt:Secret"] = "test-secret-must-be-32-chars-long-12345",
                        ["Jwt:Issuer"] = "viora-test",
                        ["Jwt:Audience"] = "viora-test",
                    });
                });
                builder.ConfigureServices(services =>
                {
                    // R3-workaround: the production PostCommitDomainEventDispatcher
                    // is registered as singleton but consumes a scoped IMediator
                    // (Cortex.Mediator's default lifetime). The pre-existing
                    // production bug only surfaces when the host is built
                    // (which never happens in unit tests). Demote the
                    // dispatcher's lifetime to scoped so the host's scope
                    // validation passes. A future change should fix this
                    // in production code (sdd-apply batch 1, obs #81).
                    services.RemoveAll<PostCommitDomainEventDispatcher>();
                    services.AddScoped<PostCommitDomainEventDispatcher>();
                });
            });
    }

    public virtual async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }
        await _postgres.DisposeAsync();
    }
}
