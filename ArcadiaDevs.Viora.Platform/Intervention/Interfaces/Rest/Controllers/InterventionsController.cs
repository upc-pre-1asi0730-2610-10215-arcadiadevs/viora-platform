using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InterventionsController(IInterventionOverviewQueryService interventionOverviewQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InterventionOverviewResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterventionsOverview(
        [FromToken] int growerId,
        CancellationToken cancellationToken = default)
    {
        var items = await interventionOverviewQueryService.Handle(
            new GetInterventionOverviewByGrowerIdQuery(growerId), cancellationToken);

        return Ok(items.Select(InterventionOverviewResourceFromDtoAssembler.ToResourceFromDto));
    }
}
