using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/specialist-dashboard")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistDashboardController(
    ISpecialistDashboardQueryService specialistDashboardQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SpecialistDashboardResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialistDashboard(
        [FromToken] int specialistId,
        CancellationToken cancellationToken = default)
    {
        var resource = await specialistDashboardQueryService.Handle(
            new GetSpecialistDashboardQuery(specialistId), cancellationToken);

        return Ok(resource);
    }
}
