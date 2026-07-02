using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/v1/authentication")]
[Produces("application/json")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Registers a new user.
    /// </summary>
    /// <remarks>
    ///     Sign-up is unconditionally open in every environment, matching OS's
    ///     ungated <c>POST /api/v1/auth/sign-up</c>. There is no admin-gate here:
    ///     no seeder anywhere assigns the Administrator role to any user, so a
    ///     production admin-only gate would be an unconditional deadlock (see
    ///     spec REQ-1).
    /// </remarks>
    [HttpPost("sign-up")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignUp(
        [FromBody] SignUpResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            user => Created($"/api/v1/users/{user?.Id}", user!.ToResource()));
    }

    /// <summary>
    ///     Authenticates a user and returns a JWT token.
    /// </summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(
        [FromBody] SignInResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            authenticatedUser => Ok(authenticatedUser.ToResource()));
    }
}
