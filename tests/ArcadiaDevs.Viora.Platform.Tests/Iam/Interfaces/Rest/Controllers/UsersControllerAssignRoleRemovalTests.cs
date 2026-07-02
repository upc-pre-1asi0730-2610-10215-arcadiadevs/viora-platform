using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArcadiaDevs.Viora.Platform;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     Regression guard for REQ-1 (spec obs #156, design obs #155 Decision 1):
///     <c>UsersController.AssignRole</c> (<c>POST /api/v1/users/{id}/roles</c>)
///     is removed entirely — OS has no equivalent endpoint. This is a real
///     HTTP-level routing test (not a controller-unit test) because there is
///     no action method left to invoke directly; the only way to prove the
///     route is gone is to observe ASP.NET Core's routing layer return 404.
/// </summary>
/// <remarks>
///     Boots the real <c>Program</c> host via <see cref="WebApplicationFactory{TEntryPoint}"/>
///     WITHOUT the Testcontainers.PostgreSql harness — the production host
///     already falls back to the EF Core InMemory provider whenever
///     <c>DATABASE_URL</c> is unset (see <c>Program.cs</c>), so this test runs
///     without Docker. A real bearer token is required: the custom
///     <c>RequestAuthorizationMiddleware</c> short-circuits with 401 (missing
///     token) for any unmatched route before ASP.NET Core's own routing 404
///     would otherwise surface, so an authenticated caller is needed to
///     actually observe the 404.
/// </remarks>
/// <remarks>
///     The host is built with <c>UseEnvironment("Testing")</c> rather than
///     the WebApplicationFactory default of "Development". This is a
///     workaround for an UNRELATED pre-existing DI lifetime bug (discovered
///     while writing this test, not introduced by it, and out of scope for
///     REQ-1/IAM): <c>AgronomicStatisticIngestionScheduler</c> is registered
///     as a singleton <c>IHostedService</c> but consumes a scoped
///     <c>IAgronomicStatisticIngestionService</c>, which fails ASP.NET
///     Core's eager <c>ValidateOnBuild</c> service-provider check — a check
///     that only runs when <c>IHostEnvironment.IsDevelopment()</c> is true.
///     Building outside Development (with the Jwt options supplied directly,
///     since the non-Development <c>appsettings.json</c> ships an empty
///     secret) sidesteps the unrelated crash without touching any
///     Agronomic-BC production code.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "InMemory")]
public class UsersControllerAssignRoleRemovalTests : IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "test-secret-must-be-32-chars-long-12345",
                        ["Jwt:Issuer"] = "viora-test",
                        ["Jwt:Audience"] = "viora-test",
                    });
                });
            });
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AssignRole_RouteRemoved_Returns404()
    {
        // GIVEN an authenticated user (sign-up + sign-in against the running host).
        // A GUID-suffixed username avoids collisions with the process-wide
        // named EF Core InMemory store shared across WebApplicationFactory
        // instances in the same test run.
        var username = $"route-removal-{Guid.NewGuid():N}";
        const string password = "long-enough-password";

        var signUpResponse = await _client.PostAsJsonAsync(
            "/api/v1/authentication/sign-up",
            new { username, password });
        Assert.Equal(HttpStatusCode.Created, signUpResponse.StatusCode);

        var signInResponse = await _client.PostAsJsonAsync(
            "/api/v1/authentication/sign-in",
            new { username, password });
        Assert.Equal(HttpStatusCode.OK, signInResponse.StatusCode);

        var signInPayload = await signInResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = signInPayload.GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // WHEN calling the removed AssignRole route
        var response = await _client.PostAsJsonAsync(
            "/api/v1/users/1/roles",
            new { roleName = "Grower" });

        // THEN the route no longer exists — 404, not 401/403 (spec REQ-1 scenario
        // "AssignRole endpoint no longer exists": "the response is 404 Not Found —
        // route no longer registered — not 403/401, since the route itself is gone").
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
