using System.Security.Claims;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Components;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Unit tests for <see cref="RequestAuthorizationMiddleware"/>.
///     Covers the five core branches: swagger path skip, AllowAnonymous skip,
///     missing Authorization header, malformed Bearer token, and valid token
///     populating HttpContext.User with role claims.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class RequestAuthorizationMiddlewareTests
{
    private static IStringLocalizer<ErrorMessages> StubErrorLocalizer()
    {
        var localizer = Substitute.For<IStringLocalizer<ErrorMessages>>();
        localizer[Arg.Any<string>()].Returns(call =>
            new LocalizedString(call.ArgAt<string>(0), value: call.ArgAt<string>(0)));
        return localizer;
    }

    private static ILogger<RequestAuthorizationMiddleware> StubLogger()
    {
        return Substitute.For<ILogger<RequestAuthorizationMiddleware>>();
    }

    private static ProblemDetailsFactory StubProblemDetailsFactory()
    {
        return new TestProblemDetailsFactory();
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
            Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary) { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };
    }

    [Fact]
    public async Task SwaggerPath_SkipsAuth()
    {
        // Arrange — path starts with /swagger → next() called, no token validation
        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new RequestAuthorizationMiddleware(next);

        var httpContext = new DefaultHttpContext
        {
            Request = { Path = new PathString("/swagger/index.html") },
            Response = { StatusCode = 200 },
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };

        // Act
        await middleware.InvokeAsync(
            httpContext,
            Substitute.For<IUserQueryService>(),
            Substitute.For<ITokenService>(),
            StubErrorLocalizer(),
            StubProblemDetailsFactory(),
            StubLogger());

        // Assert — next was called, no token validation happened
        Assert.True(nextCalled);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task AllowAnonymousEndpoint_SkipsAuth()
    {
        // Arrange — endpoint decorated with [AllowAnonymous] → next() called
        var nextCalled = false;
        RequestDelegate next = ctx => { nextCalled = true; return Task.CompletedTask; };
        var middleware = new RequestAuthorizationMiddleware(next);

        // AllowAnonymousAttribute is a plain Attribute (not IFilterMetadata),
        // but the middleware checks via GetType() equality on the object collection.
        var endpointMetadata = new EndpointMetadataCollection(new object[] { new AllowAnonymousAttribute() });
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            endpointMetadata,
            "test-endpoint");

        var httpContext = new DefaultHttpContext
        {
            Request = { Path = new PathString("/api/test") },
            Response = { StatusCode = 200 },
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };
        httpContext.SetEndpoint(endpoint);

        // Act
        await middleware.InvokeAsync(
            httpContext,
            Substitute.For<IUserQueryService>(),
            Substitute.For<ITokenService>(),
            StubErrorLocalizer(),
            StubProblemDetailsFactory(),
            StubLogger());

        // Assert — next was called, no token validation
        Assert.True(nextCalled);
        Assert.Equal(200, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task NoAuthorizationHeader_ReturnsTokenRequired()
    {
        // Arrange — no Authorization header → 401 with IamErrors.TokenRequired
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestAuthorizationMiddleware(next);

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Path = new PathString("/api/test"),
                Headers = { }, // no Authorization header
            },
            Response = { StatusCode = 0, Body = new MemoryStream() },
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };

        // Act
        await middleware.InvokeAsync(
            httpContext,
            Substitute.For<IUserQueryService>(),
            Substitute.For<ITokenService>(),
            StubErrorLocalizer(),
            StubProblemDetailsFactory(),
            StubLogger());

        // Assert — 401 Unauthorized with TokenRequired error code
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains(IamErrors.TokenRequired.Code, body);
    }

    [Fact]
    public async Task MalformedBearer_ReturnsTokenMalformed()
    {
        // Arrange — "Bearer " with no token → 401 with IamErrors.TokenMalformed
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestAuthorizationMiddleware(next);

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Path = new PathString("/api/test"),
                Headers = { ["Authorization"] = "Bearer " },
            },
            Response = { StatusCode = 0, Body = new MemoryStream() },
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };

        // Act
        await middleware.InvokeAsync(
            httpContext,
            Substitute.For<IUserQueryService>(),
            Substitute.For<ITokenService>(),
            StubErrorLocalizer(),
            StubProblemDetailsFactory(),
            StubLogger());

        // Assert — 401 with TokenMalformed error code
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        Assert.Contains(IamErrors.TokenMalformed.Code, body);
    }

    [Fact]
    public async Task ValidToken_PopulatesUserWithRoleClaims()
    {
        // Arrange — valid token → HttpContext.User populated with role claims
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = new RequestAuthorizationMiddleware(next);

        // Build a fake user with roles
        var createResult = Role.Create("Grower");
        var role = ((Result<Role, Error>.Success)createResult).Value;
        var user = new User("testuser", "hash");
        user.Roles.Add(role);

        // Stub token service → valid token
        var tokenService = Substitute.For<ITokenService>();
        tokenService.ValidateToken("valid-jwt")
            .Returns(JwtValidationResult.Success(userId: 42));

        // Stub user query service → returns the user
        var userQueryService = Substitute.For<IUserQueryService>();
        userQueryService.Handle(Arg.Any<GetUserByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var httpContext = new DefaultHttpContext
        {
            Request =
            {
                Path = new PathString("/api/test"),
                Headers = { ["Authorization"] = "Bearer valid-jwt" },
            },
            Response = { StatusCode = 200 },
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };

        // Act
        await middleware.InvokeAsync(
            httpContext,
            userQueryService,
            tokenService,
            StubErrorLocalizer(),
            StubProblemDetailsFactory(),
            StubLogger());

        // Assert — HttpContext.User has role claim
        Assert.True(httpContext.User.Identity?.IsAuthenticated);
        Assert.True(httpContext.User.IsInRole("Grower"));

        // Assert — Items populated for downstream middleware
        Assert.Equal(user, httpContext.Items["User"]);
        Assert.Equal(user.Id, httpContext.Items["UserId"]);
        Assert.Equal(user.Username, httpContext.Items["Username"]);

        // Assert — next was called
        Assert.Equal(200, httpContext.Response.StatusCode);
    }
}
