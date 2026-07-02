using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for role operations.
/// </summary>
[ApiController]
[Route("api/v1/roles")]
[Produces("application/json")]
[Authorize]
public class RolesController(IRoleQueryService roleQueryService) : ControllerBase
{
    /// <summary>
    ///     Gets all roles.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var roles = await roleQueryService.Handle(new GetAllRolesQuery(), cancellationToken);
        var resources = roles.Select(role => role.ToResource());
        return Ok(resources);
    }
}
