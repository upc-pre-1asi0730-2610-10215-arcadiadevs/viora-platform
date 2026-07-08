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
