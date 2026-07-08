using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
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
using System.Security.Claims;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for user operations.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Authorize]
public class UsersController(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Gets all users.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Users returned (possibly empty).</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userQueryService.Handle(new GetAllUsersQuery(), cancellationToken);
        var resources = users.Select(user => user.ToResource());
        return Ok(resources);
    }

    /// <summary>
    ///     Gets the authenticated user (the user behind the bearer token).
    /// </summary>
    /// <remarks>
    ///     The <c>sid</c> claim is populated by
    ///     <c>RequestAuthorizationMiddleware</c> from the validated JWT, so by
    ///     the time the action runs, the user is guaranteed authenticated. If
    ///     the user has been deleted between token issuance and request time,
    ///     the lookup returns null and we surface a 404 with the standard
    ///     ProblemDetails envelope.
    /// </remarks>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.Sid) ?? User.FindFirstValue("sub");
        if (!int.TryParse(sub, out var id))
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status401Unauthorized,
                IamErrors.TokenRequired.Code,
                errorLocalizer[IamErrors.TokenRequired.Code].Value ?? IamErrors.TokenRequired.Message);
            return Unauthorized(problemDetails);
        }

        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);
        if (user == null)
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status404NotFound,
                IamErrors.UserNotFound.Code,
                errorLocalizer[IamErrors.UserNotFound.Code].Value ?? IamErrors.UserNotFound.Message);
            return NotFound(problemDetails);
        }

        return Ok(user.ToResource());
    }

    /// <summary>
    ///     Gets a user by ID.
    /// </summary>
    /// <param name="id">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">User found.</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(id), cancellationToken);

        if (user == null)
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status404NotFound,
                IamErrors.UserNotFound.Code,
                errorLocalizer[IamErrors.UserNotFound.Code].Value ?? IamErrors.UserNotFound.Message);
            return NotFound(problemDetails);
        }

        return Ok(user.ToResource());
    }

    /// <summary>
    ///     Changes a user's password.
    /// </summary>
    /// <remarks>
    ///     This endpoint is bearerAuth-only with no self-only/ownership guard on
    ///     <paramref name="userId"/> — any authenticated caller may change any
    ///     user's password.
    /// </remarks>
    [HttpPut("{userId:int}/password")]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromRoute] int userId,
        [FromBody] ChangePasswordResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand(userId);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            user => Ok(user!.ToResource()));
    }

    /// <summary>
    ///     Deactivates a user's account (REQ-DEACT-2). Danger-zone semantics —
    ///     only <c>{ "active": false }</c> is accepted; no reactivation path
    ///     exists via this endpoint.
    /// </summary>
    /// <remarks>
    ///     No self-only/ownership guard on <paramref name="userId"/> — same
    ///     inherited-risk idiom as <see cref="ChangePassword"/>/<see cref="GetById"/>.
    /// </remarks>
    [HttpPatch("{userId:int}")]
    [ProducesResponseType(typeof(UserResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateActiveState(
        [FromRoute] int userId,
        [FromBody] UpdateUserResource resource,
        CancellationToken cancellationToken)
    {
        // REQ-DEACT-2: active:true or omitted/null is rejected before the
        // aggregate is ever touched — WA returns a proper ProblemDetails
        // envelope here (diverges from OS's bare 400), matching this repo's
        // own hard REST-compliance convention.
        if (resource.Active != false)
        {
            var invalidResult = new Result<User?, Error>.Failure(IamErrors.InvalidAccountStateUpdate);
            return IamActionResultAssembler.ToActionResult(
                this,
                invalidResult,
                errorLocalizer,
                problemDetailsFactory,
                user => Ok(user!.ToResource()));
        }

        var command = new DeactivateUserCommand(userId);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            user => Ok(user!.ToResource()));
    }

    /// <summary>
    ///     Permanently deletes a user's account (Danger zone): the Iam user,
    ///     their sessions and verification tokens, and their profile.
    /// </summary>
    /// <remarks>
    ///     No self-only/ownership guard on <paramref name="userId"/> — same as
    ///     <see cref="ChangePassword"/>/<see cref="UpdateActiveState"/>. This cannot be undone.
    /// </remarks>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Account deleted.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccount(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand(userId);
        var result = await userCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            _ => NoContent());
    }
}
