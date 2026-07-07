using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArcadiaDevs.Viora.Platform;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     Regression guard for the priority-3 OS-WA parity item "Iam verification
///     routes renamed to <c>/verifications</c> and <c>/verification-requests</c>"
///     (commit a8c2700, spec: docs/os-wa-parity-audit-2026-07-05.md). Before
///     that fix, <c>AuthenticationController</c> exposed <c>POST /verify</c>
///     and <c>POST /resend-verification</c>; OS renamed the equivalent
///     endpoints to <c>/verifications</c> and <c>/verification-requests</c>
///     and WA's controller drifted from that rename until this fix.
/// </summary>
/// <remarks>
///     Real HTTP-level tests via <see cref="WebApplicationFactory{TEntryPoint}"/>
///     WITHOUT Docker/Testcontainers — the host falls back to the EF Core
///     InMemory provider ("VioraPlatform", a fixed name shared process-wide,
///     see <c>Program.cs</c>) whenever <c>DATABASE_URL</c> is unset. Built with
///     <c>UseEnvironment("Testing")</c> for the same reason as
///     <c>UsersControllerAssignRoleRemovalTests</c>: building outside
///     Development sidesteps an unrelated <c>ValidateOnBuild</c> DI crash in
///     <c>AgronomicStatisticIngestionScheduler</c> that only triggers when
///     <c>IHostEnvironment.IsDevelopment()</c> is true.
/// </remarks>
/// <remarks>
///     The verification token itself is never returned by any API response
///     (it is only emailed, and <c>BrevoEmailService</c> no-ops/logs when
///     unconfigured — see its Testing-environment comment). Tests that need
///     a real token reach into the running host's DI container via
///     <c>_factory.Services.CreateScope()</c> and query
///     <see cref="IVerificationTokenRepository"/> directly — this is the same
///     "VioraPlatform" named InMemory store the controller action used, so
///     the row is visible immediately after the HTTP call completes.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Database", "InMemory")]
public class VerificationRoutesTests : IAsyncLifetime
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

    /// <summary>
    ///     Signs up a fresh, unverified user (GUID-suffixed username, same
    ///     collision-avoidance rationale as <c>UsersControllerAssignRoleRemovalTests</c>
    ///     — the InMemory store is a single named database shared across the
    ///     whole test process) and returns the username plus the real
    ///     verification token fetched directly from the repository.
    /// </summary>
    private async Task<(string Username, string Token)> SignUpUnverifiedUserAsync()
    {
        var username = $"verif-route-{Guid.NewGuid():N}";
        const string password = "long-enough-password";

        var signUpResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/sign-up",
            new { username, password, email = $"{username}@example.com", fullName = "Verification Route Test User" });
        Assert.Equal(HttpStatusCode.Created, signUpResponse.StatusCode);

        var signUpPayload = await signUpResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userId = signUpPayload.GetProperty("id").GetInt32();

        using var scope = _factory.Services.CreateScope();
        var verificationTokenRepository = scope.ServiceProvider.GetRequiredService<IVerificationTokenRepository>();
        var tokens = await verificationTokenRepository.FindByUserIdAsync(userId, CancellationToken.None);
        var token = Assert.Single(tokens).Token;

        return (username, token);
    }

    [Fact]
    public async Task Verify_NewRoute_ConsumesTokenAndReturnsAuthenticatedUser()
    {
        // GIVEN a freshly signed-up, unverified user and their real verification token
        var (username, token) = await SignUpUnverifiedUserAsync();

        // WHEN calling the renamed verification route
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/verifications",
            new { token });

        // THEN the account is verified and an authenticated session is returned (REQ-EV-2)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(username, payload.GetProperty("username").GetString());
        Assert.False(string.IsNullOrEmpty(payload.GetProperty("token").GetString()));
    }

    [Fact]
    public async Task Verify_NewRoute_UnknownToken_Returns404()
    {
        // WHEN calling the renamed verification route with a token that was never issued
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/verifications",
            new { token = $"unknown-{Guid.NewGuid():N}" });

        // THEN Iam.VerificationTokenNotFound maps to 404 (IamActionResultAssembler)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Verify_NewRoute_AlreadyConsumedToken_Returns400()
    {
        // GIVEN a token that has already been consumed once
        var (_, token) = await SignUpUnverifiedUserAsync();
        var firstAttempt = await _client.PostAsJsonAsync(
            "/api/v1/auth/verifications",
            new { token });
        Assert.Equal(HttpStatusCode.OK, firstAttempt.StatusCode);

        // WHEN consuming the same token again
        var secondAttempt = await _client.PostAsJsonAsync(
            "/api/v1/auth/verifications",
            new { token });

        // THEN Iam.VerificationTokenExpiredOrConsumed maps to 400 (REQ-EV-2)
        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_NewRoute_IssuesNewTokenForUnverifiedUser()
    {
        // GIVEN a freshly signed-up, unverified user
        var (username, _) = await SignUpUnverifiedUserAsync();

        // WHEN calling the renamed resend route
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/verification-requests",
            new { username });

        // THEN a fresh verification token is issued and the user resource is returned (REQ-EV-3)
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(username, payload.GetProperty("username").GetString());
        Assert.False(payload.GetProperty("verified").GetBoolean());
    }

    [Fact]
    public async Task ResendVerification_NewRoute_UnknownUsername_Returns404()
    {
        // WHEN calling the renamed resend route for an account that does not exist
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/verification-requests",
            new { username = $"nonexistent-{Guid.NewGuid():N}" });

        // THEN Iam.UserNotFound maps to 404 (IamActionResultAssembler)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ResendVerification_NewRoute_AlreadyVerifiedAccount_Returns422()
    {
        // GIVEN an account that has already completed verification
        var (username, token) = await SignUpUnverifiedUserAsync();
        var verifyResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/verifications",
            new { token });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        // WHEN requesting another verification token for that already-verified account
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/verification-requests",
            new { username });

        // THEN Iam.EmailAlreadyVerified maps to 422 (REQ-EV-3)
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task OldVerbBasedRoutes_AreRemoved_Returns404()
    {
        // GIVEN an authenticated caller (a real bearer token is required: unmatched
        // routes are 401'd by RequestAuthorizationMiddleware before ASP.NET Core's
        // own routing 404 would otherwise surface — same rationale as
        // UsersControllerAssignRoleRemovalTests). Verify's auto sign-in (REQ-EV-2)
        // is used to obtain the token without needing a separate sign-in call,
        // since a freshly signed-up user cannot sign in until verified.
        var (_, token) = await SignUpUnverifiedUserAsync();
        var verifyResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/verifications",
            new { token });
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<JsonElement>();
        var bearerToken = verifyPayload.GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        // WHEN calling the old, pre-rename verb-based paths
        var oldVerifyResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/verify",
            new { token = "irrelevant" });
        var oldResendResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/resend-verification",
            new { username = "irrelevant" });

        // THEN neither old route is registered anymore — 404, not 400/401/403
        Assert.Equal(HttpStatusCode.NotFound, oldVerifyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, oldResendResponse.StatusCode);
    }
}
