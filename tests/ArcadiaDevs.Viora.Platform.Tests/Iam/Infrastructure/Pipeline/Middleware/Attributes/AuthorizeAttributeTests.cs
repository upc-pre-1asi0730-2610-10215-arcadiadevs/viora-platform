using System.Security.Claims;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Infrastructure.Pipeline.Middleware.Attributes;

/// <summary>
///     Covers REQ-1 (obs #156): "Every <c>[Authorize(Roles = "...")]</c>
///     attribute referencing these 2 roles MUST use the new strings" and
///     the scenario "Role rename still applies to the 2 surviving roles".
///     <para>
///         Deviation note (T-1 pre-check, obs #158 T-1..T-8): an exhaustive
///         grep of the app source found ZERO
///         <c>[Authorize(Roles = "OliveProducer"|"PhytosanitarySpecialist")]</c>
///         literals anywhere in the codebase to rename — the only
///         <c>[Authorize(Roles = ...)]</c> literal in the entire app is
///         <c>[Authorize(Roles = "Administrator")]</c> on
///         <c>UsersController.AssignRole</c>, which is out of this batch's
///         scope (deleted whole-cloth in T-14, a later batch). T-8's
///         "sweep and rename" therefore has no production literal to
///         change. This test class instead adds the regression coverage
///         <see cref="AuthorizeAttribute"/> itself was missing (flagged by
///         static analysis as having zero covering tests) so any future
///         re-introduction of the retired role strings is caught here.
///     </para>
/// </summary>
public class AuthorizeAttributeTests
{
    private static IStringLocalizer<ErrorMessages> StubLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
    }

    /// <summary>
    ///     Builds an <see cref="AuthorizationFilterContext"/> for a signed-in
    ///     caller carrying <paramref name="roleClaimValue"/> as their
    ///     <see cref="ClaimTypes.Role"/> claim. Mirrors the
    ///     <c>RequestAuthorizationMiddleware</c> contract: an authenticated
    ///     caller has both <c>HttpContext.User</c> populated AND
    ///     <c>HttpContext.Items["User"]</c> set (the attribute treats a null
    ///     <c>Items["User"]</c> as "not authenticated" regardless of
    ///     <c>HttpContext.User</c>).
    /// </summary>
    private static AuthorizationFilterContext BuildContext(string roleClaimValue)
    {
        var services = new ServiceCollection();
        services.AddSingleton(StubLocalizer());
        services.AddSingleton<ProblemDetailsFactory>(new TestProblemDetailsFactory());

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, roleClaimValue) }, "test");
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity),
            RequestServices = services.BuildServiceProvider(),
        };
        httpContext.Items["User"] = "authenticated-user-marker"; // any non-null value

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void OnAuthorization_RoleClaimMatchesGrower_Allows()
    {
        // GIVEN a caller whose role claim is the new OS-aligned name "Grower"
        var context = BuildContext("Grower");
        var sut = new AuthorizeAttribute { Roles = "Grower,Specialist" };

        // WHEN authorization runs
        sut.OnAuthorization(context);

        // THEN access is allowed — no Result was set
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_RoleClaimMatchesSpecialist_Allows()
    {
        // GIVEN a caller whose role claim is the new OS-aligned name "Specialist"
        var context = BuildContext("Specialist");
        var sut = new AuthorizeAttribute { Roles = "Grower,Specialist" };

        // WHEN authorization runs
        sut.OnAuthorization(context);

        // THEN access is allowed
        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_RoleClaimIsRetiredOliveProducerName_Returns403()
    {
        // GIVEN a caller whose role claim is the RETIRED pre-migration name
        // "OliveProducer" (regression guard: the renamed attribute must
        // NOT accept the old string once REQ-1 has landed).
        var context = BuildContext("OliveProducer");
        var sut = new AuthorizeAttribute { Roles = "Grower,Specialist" };

        // WHEN authorization runs
        sut.OnAuthorization(context);

        // THEN access is denied with 403
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void OnAuthorization_RoleClaimIsRemovedAdministratorName_Returns403()
    {
        // GIVEN a caller whose role claim is the entirely-removed
        // "Administrator" role (regression guard: no surviving code path
        // may special-case this retired role name).
        var context = BuildContext("Administrator");
        var sut = new AuthorizeAttribute { Roles = "Grower,Specialist" };

        // WHEN authorization runs
        sut.OnAuthorization(context);

        // THEN access is denied with 403
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
}
