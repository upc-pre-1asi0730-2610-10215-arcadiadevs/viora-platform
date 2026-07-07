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
[Route("api/v1/auth")]
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
    /// <param name="resource">The sign-up payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">User created.</response>
    /// <response code="400">Validation failure.</response>
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
    /// <param name="resource">The sign-in credentials.</param>
    /// <param name="userAgent">The requesting client's user agent, recorded on the resulting session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Authentication succeeded.</response>
    /// <response code="401">Invalid credentials.</response>
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

    /// <summary>
    ///     Consumes a verification token and marks the account as verified.
    ///     Auto signs-in on success (REQ-EV-2).
    /// </summary>
    /// <param name="resource">The verification token payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Account verified; authenticated session returned.</response>
    /// <response code="404">Token not found.</response>
    /// <response code="400">Token expired or already used.</response>
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

    /// <summary>
    ///     Issues a new verification token for an unverified account (REQ-EV-3).
    /// </summary>
    /// <param name="resource">The account identifier to resend a verification token for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Verification token reissued.</response>
    /// <response code="404">Account not found.</response>
    /// <response code="422">Account is already verified.</response>
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
