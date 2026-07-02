using System.Reflection;
using System.Security.Claims;
using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Controllers;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Interfaces.Rest.Controllers;

/// <summary>
/// Tests for the new <c>GET /api/v1/users/me</c> endpoint added by IAM-001.
/// </summary>
/// <remarks>
/// These are controller-level unit tests: they construct the controller with
/// NSubstitute mocks and call the action directly, bypassing the
/// <c>[Authorize]</c> filter (which the integration path enforces — verified
/// separately by reflection).
/// </remarks>
public class UsersControllerGetMeTests
{
    private readonly IUserQueryService _userQueryService = Substitute.For<IUserQueryService>();
    private readonly IUserCommandService _userCommandService = Substitute.For<IUserCommandService>();
    private readonly IStringLocalizer<ArcadiaDevs.Viora.Platform.Shared.Resources.Errors.ErrorMessages> _errorLocalizer =
        StubLocalizer();
    private readonly ProblemDetailsFactory _problemDetailsFactory = new TestProblemDetailsFactory();

    /// <summary>
    /// Builds a localizer stub that returns a real <see cref="LocalizedString"/>
    /// for any key — the NSubstitute default for an indexer is null, which
    /// would NRE the controller's <c>errorLocalizer[...].Value ?? fallback</c>
    /// pattern.
    /// </summary>
    private static IStringLocalizer<ArcadiaDevs.Viora.Platform.Shared.Resources.Errors.ErrorMessages> StubLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ArcadiaDevs.Viora.Platform.Shared.Resources.Errors.ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    private static ProblemDetailsFactory CreateStubProblemDetailsFactory()
    {
        // NSubstitute's default for an abstract/virtual method is `null`, which
        // would NRE the controller's `CreateProblemDetails(...)` call. We
        // hand-roll a minimal subclass that returns a real ProblemDetails
        // for every overload — robust against overload-resolution quirks.
        return new TestProblemDetailsFactory();
    }

    // Defensive getter: the field initializer above may run before the nested
    // class is type-resolved on some runtimes. Returning a fresh instance from
    // here guarantees the controller never sees a null factory.
    private ProblemDetailsFactory ProblemDetailsFactorySafe =>
        _problemDetailsFactory ?? new TestProblemDetailsFactory();

    /// <summary>
    /// Minimal concrete <see cref="ProblemDetailsFactory"/> for unit tests.
    /// Returns a non-null <see cref="ProblemDetails"/> for every call, populated
    /// from the status / title / type / detail arguments when present.
    /// </summary>
    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };
    }

    /// <summary>
    /// Sets the read-only <see cref="User.Id"/> via reflection. EF Core uses
    /// the same trick when materializing the aggregate from the database.
    /// </summary>
    private static User CreateUserWithId(int id, string username) =>
        SetBackingField(new User(username, "irrelevant-hash"), id);

    private static User SetBackingField(User user, int id)
    {
        // Id is declared as { get; } (auto-property), so the compiler emits a
        // private readonly backing field. We poke through reflection because
        // that's exactly what EF does at materialization time.
        var idField = typeof(User).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(idField);
        idField!.SetValue(user, id);
        return user;
    }

    private UsersController CreateControllerWithUser(int userId, string username)
    {
        var controller = new UsersController(
            _userQueryService,
            _userCommandService,
            _errorLocalizer,
            ProblemDetailsFactorySafe);

        // The RequestAuthorizationMiddleware populates HttpContext.Items["User"]
        // and HttpContext.User with claims. The /me handler reads the Sid claim.
        var claims = new[]
        {
            new Claim(ClaimTypes.Sid, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal,
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        httpContext.Items["User"] = CreateUserWithId(userId, username);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
        return controller;
    }

    [Fact]
    public async Task GetMe_WithValidSidClaim_ReturnsOkWithUserResource()
    {
        // GIVEN a controller wired to a query service that returns a known user
        const int userId = 42;
        const string username = "alice";
        var user = CreateUserWithId(userId, username);
        _userQueryService.Handle(Arg.Is<GetUserByIdQuery>(q => q.Id == userId),
                                 Arg.Any<CancellationToken>())
                         .Returns(user);

        var controller = CreateControllerWithUser(userId, username);

        // WHEN calling GetMe
        var result = await controller.GetMe(CancellationToken.None);

        // THEN the result is 200 OK with a UserResource reflecting the authenticated user
        var ok = Assert.IsType<OkObjectResult>(result);
        var resource = Assert.IsType<UserResource>(ok.Value);
        Assert.Equal(userId, resource.Id);
        Assert.Equal(username, resource.Username);
    }

    [Fact]
    public async Task GetMe_WhenUserLookupReturnsNull_ReturnsNotFound()
    {
        // GIVEN a controller wired to a query service that returns null
        // (e.g. user was deleted between token issuance and request time)
        const int userId = 42;
        _userQueryService.Handle(Arg.Is<GetUserByIdQuery>(q => q.Id == userId),
                                 Arg.Any<CancellationToken>())
                         .Returns((User?)null);

        var controller = CreateControllerWithUser(userId, "alice");

        // WHEN calling GetMe
        var result = await controller.GetMe(CancellationToken.None);

        // THEN the result is 404 NotFound
        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        // AND the body is the standard ProblemDetails envelope (the type comes from MVC, not our resource)
        Assert.IsType<ProblemDetails>(notFound.Value);
    }

    [Fact]
    public void GetMe_IsProtectedByAuthorizeAttribute()
    {
        // GIVEN the UsersController type
        var method = typeof(UsersController).GetMethod(nameof(UsersController.GetMe));

        // THEN it exists and is decorated with [Authorize]
        Assert.NotNull(method);
        var hasAuthorize = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true).Any();
        Assert.True(hasAuthorize, "GET /api/v1/users/me must be protected by [Authorize] (IAM-001).");

        // AND the class-level [Authorize] on UsersController is the second line of defence (the
        // middleware-level filter for HTTP bearer tokens). Without a token, the
        // RequestAuthorizationMiddleware short-circuits the pipeline with 401 before
        // the action is reached.
        var classAuthorize = typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Any();
        Assert.True(classAuthorize, "UsersController must declare [Authorize] at the class level.");
    }
}
