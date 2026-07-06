using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
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
///     REST controller for user-session operations (REQ-SESS-2, REQ-SESS-3).
/// </summary>
/// <remarks>
///     No self-only/ownership guard on <c>userId</c> — matches the same
///     already-documented inherited-risk idiom as
///     <see cref="UsersController.ChangePassword" />/<see cref="UsersController.GetById" />,
///     not a new gap introduced here.
/// </remarks>
[ApiController]
[Route("api/v1/users/{userId:int}/sessions")]
[Produces("application/json")]
[Authorize]
public class UserSessionsController(
    ISessionQueryService sessionQueryService,
    ISessionCommandService sessionCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Lists sessions belonging to the given user.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Sessions returned (possibly empty).</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserSessionResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromRoute] int userId,
        CancellationToken cancellationToken)
    {
        var sessions = await sessionQueryService.Handle(new GetUserSessionsQuery(userId), cancellationToken);
        var resources = sessions.Select(session => session.ToResource());
        return Ok(resources);
    }

    /// <summary>
    ///     Revokes a session belonging to the given user (REQ-SESS-3).
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="sessionId">The session id to revoke.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Session revoked.</response>
    /// <response code="401">Missing or invalid bearer token.</response>
    /// <response code="404">Session not found.</response>
    /// <response code="409">Session already revoked.</response>
    [HttpDelete("{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Revoke(
        [FromRoute] int userId,
        [FromRoute] int sessionId,
        CancellationToken cancellationToken)
    {
        var command = new RevokeSessionCommand(userId, sessionId);
        var result = await sessionCommandService.Handle(command, cancellationToken);

        return IamActionResultAssembler.ToActionResult<Unit>(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            _ => Ok(sessionId));
    }
}
