using System.Security.Claims;
using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Controllers;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     Tests for REQ-1 (spec obs #156): <c>AuthenticationController.SignUp</c>
///     MUST become unconditionally open in every environment — the prior
///     Production-only <c>IsInRole("Administrator")</c> gate is removed
///     entirely because no seeder anywhere ever assigns the Administrator
///     role to any user, making the gate an unconditional sign-up deadlock.
/// </summary>
public class AuthenticationControllerTests
{
    private readonly IUserCommandService _userCommandService = Substitute.For<IUserCommandService>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer = StubLocalizer();
    private readonly ProblemDetailsFactory _problemDetailsFactory = new TestProblemDetailsFactory();

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
    ///     Builds a controller with an anonymous <see cref="ClaimsPrincipal"/>
    ///     attached to the <see cref="HttpContext"/> — equivalent to a request
    ///     with no <c>Authorization</c> header, since
    ///     <c>RequestAuthorizationMiddleware</c> never runs for
    ///     <c>[AllowAnonymous]</c> requests without a token. There is
    ///     deliberately no environment-name wiring: SignUp no longer branches
    ///     on <c>IWebHostEnvironment</c> at all (spec REQ-1).
    /// </summary>
    private AuthenticationController CreateProductionControllerWithNoAuthHeader()
    {
        var controller = new AuthenticationController(
            _userCommandService,
            _errorLocalizer,
            _problemDetailsFactory);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()), // anonymous — no claims, no roles
            RequestServices = new ServiceCollection().BuildServiceProvider(),
        };

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }

    [Fact]
    public async Task SignUp_ProductionNoAuthHeader_Returns201()
    {
        // GIVEN the Production environment, no Authorization header (anonymous caller),
        // and a command service that will succeed
        var user = new User("newgrower", "irrelevant-hash");
        _userCommandService
            .Handle(Arg.Any<SignUpCommand>(), Arg.Any<CancellationToken>())
            .Returns(new Result<User?, Error>.Success(user));

        var controller = CreateProductionControllerWithNoAuthHeader();
        var resource = new SignUpResource("newgrower", "long-enough-password");

        // WHEN calling SignUp
        var result = await controller.SignUp(resource, CancellationToken.None);

        // THEN the request succeeds — sign-up is unconditionally open, matching OS's
        // ungated POST /api/v1/auth/sign-up (spec REQ-1, scenario "Sign-up succeeds
        // for an anonymous caller in production"). The prior behavior (this scenario
        // returning 403 Iam.SignUpRequiresAdmin) was an unconditional deadlock: no
        // seeder ever assigns the Administrator role to any user, so nobody —
        // including a would-be first administrator — could ever pass this gate.
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        // AND the command service was actually invoked (the gate did not short-circuit)
        await _userCommandService.Received(1).Handle(Arg.Any<SignUpCommand>(), Arg.Any<CancellationToken>());
    }
}
