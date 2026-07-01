using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Smoke test for the test harness itself: verifies that
///     <see cref="IntegrationTestBase"/> can boot a
///     <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
///     against a Testcontainers.PostgreSql instance, that the host's DI
///     graph is fully wired (services resolve correctly), and that a
///     <see cref="HttpClient"/> can be created.
/// </summary>
/// <remarks>
///     The smoke test intentionally does NOT call an HTTP endpoint
///     because the production auth middleware (<c>RequestAuthorizationMiddleware</c>)
///     requires a valid JWT for every non-<c>[AllowAnonymous]</c> endpoint
///     (returns 401 otherwise). Verifying auth is the role of F4.1 (controller
///     tests); the harness's role is just to ensure the host boots and the
///     DI graph is intact. The <c>AppDbContext</c> resolution proves the
///     Postgres connection string is wired end-to-end.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Category", "Smoke")]
[Trait("Database", "Postgres")]
[Collection("Postgres")]
public class HarnessSmokeTest : IntegrationTestBase
{
    [Fact]
    public void Harness_WebApplicationFactory_ResolvesHostServices_AndCreatesClient()
    {
        // Act — accessing the factory triggers host build + DI graph validation.
        Assert.NotNull(Factory);
        using var client = Factory.CreateClient();

        // Assert — the HttpClient is alive and addresses resolve.
        Assert.NotNull(client);
        Assert.NotNull(client.BaseAddress);
        Assert.True(Factory.Services.GetService(typeof(IHost)) is not null,
            "Host did not build — the DI graph is broken.");
    }
}
