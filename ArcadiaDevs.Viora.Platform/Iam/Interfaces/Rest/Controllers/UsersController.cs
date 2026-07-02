using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
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
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Gets all users.
    /// </summary>
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
}
