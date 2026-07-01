using System.Net.Http;

namespace ArcadiaDevs.Viora.Platform.Tests.TestHarness;

/// <summary>
///     Test authentication helper that injects fake-user headers into
///     every request issued by an <see cref="HttpClient"/>. The
///     <see cref="TestAuthMiddleware"/> (registered by the harness)
///     reads these headers and populates
///     <c>HttpContext.Items["User"]</c> + <c>HttpContext.User</c>,
///     matching the contract that the production
///     <c>RequestAuthorizationMiddleware</c> would have set after
///     validating a real JWT.
/// </summary>
/// <remarks>
///     <para>
///         This is the <c>TestAuthHelper</c> from design §1.4. The
///         header-based test path is auth-agnostic: the future
///         <c>Microsoft.AspNetCore.Authentication.JwtBearer</c>
///         migration (SHARED-015) will replace the production
///         middleware, but the test path stays the same because
///         the boundary is <c>HttpContext.Items["User"]</c>.
///     </para>
///     <para>
///         Header names are <see cref="UserIdHeader"/>,
///         <see cref="UsernameHeader"/>, and <see cref="RolesHeader"/>
///         (the latter as a comma-separated list).
///     </para>
/// </remarks>
public static class TestAuthHelper
{
    public const string UserIdHeader = "X-Test-User-Id";
    public const string UsernameHeader = "X-Test-User-Name";
    public const string RolesHeader = "X-Test-User-Roles";

    /// <summary>
    ///     Sets the test-user headers on <paramref name="client"/>'s
    ///     <see cref="HttpClient.DefaultRequestHeaders"/>. After this
    ///     call, every request issued by the client will carry the
    ///     user identity that <see cref="TestAuthMiddleware"/> will
    ///     read.
    /// </summary>
    public static HttpClient WithTestUser(
        this HttpClient client,
        long userId,
        string username,
        IEnumerable<string>? roles = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("username must not be empty", nameof(username));
        }

        client.DefaultRequestHeaders.Remove(UserIdHeader);
        client.DefaultRequestHeaders.Add(UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Remove(UsernameHeader);
        client.DefaultRequestHeaders.Add(UsernameHeader, username);
        if (roles is not null)
        {
            var distinct = roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToArray();
            if (distinct.Length > 0)
            {
                client.DefaultRequestHeaders.Remove(RolesHeader);
                client.DefaultRequestHeaders.Add(RolesHeader, string.Join(',', distinct));
            }
        }
        return client;
    }
}
