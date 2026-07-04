using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for the composed producer-facing intervention
///     overview (REQ-OV-1, REQ-OV-2).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InterventionsController(IInterventionOverviewQueryService interventionOverviewQueryService) : ControllerBase
{
    /// <summary>
    ///     Gets the composed overview of every intervention request for a
    ///     grower, including the downstream chain and a derived status.
    /// </summary>
    /// <param name="growerId">The grower id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Overview rows returned (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InterventionOverviewResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterventionsOverview(
        [FromQuery] int growerId,
        CancellationToken cancellationToken = default)
    {
        var items = await interventionOverviewQueryService.Handle(
            new GetInterventionOverviewByGrowerIdQuery(growerId), cancellationToken);

        return Ok(items.Select(InterventionOverviewResourceFromDtoAssembler.ToResourceFromDto));
    }
}
