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

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
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

    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SignIn(
        [FromBody] SignInResource resource,
        [FromHeader(Name = "User-Agent")] string? userAgent,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand(userAgent);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            authenticatedUser => Ok(authenticatedUser.ToResource()));
    }

    [HttpPost("verifications")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthenticatedUserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyResource resource,
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

    [HttpPost("verification-requests")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            user => Ok(user!.ToResource()));
    }
}
