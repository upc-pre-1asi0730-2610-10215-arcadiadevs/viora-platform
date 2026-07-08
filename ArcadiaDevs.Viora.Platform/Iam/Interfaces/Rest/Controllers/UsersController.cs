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
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await userQueryService.Handle(new GetAllUsersQuery(), cancellationToken);
        var resources = users.Select(user => user.ToResource());
        return Ok(resources);
    }

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
