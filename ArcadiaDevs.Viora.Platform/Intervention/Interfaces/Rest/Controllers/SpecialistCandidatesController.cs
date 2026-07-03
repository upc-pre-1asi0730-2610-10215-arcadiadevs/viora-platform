using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for specialist candidate matching (REQ-SPEC-3).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistCandidatesController(ISpecialistQueryService specialistQueryService) : ControllerBase
{
    /// <summary>
    ///     Get ranked specialist candidates for an alert.
    /// </summary>
    /// <remarks>
    ///     Ranks by availability → successRate desc → distanceKm asc,
    ///     capped to <paramref name="limit" /> (default 3). <paramref name="alertId" />
    ///     is accepted as a reserved parameter (unused for ranking today,
    ///     OS parity) — never rejected.
    /// </remarks>
    /// <param name="alertId">Reserved parameter, currently unused for ranking.</param>
    /// <param name="limit">The maximum number of candidates to return (default 3).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Ranked candidates returned (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpecialistResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSpecialistCandidates(
        [FromQuery] long? alertId,
        [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        var candidates = await specialistQueryService.Handle(
            new GetSpecialistCandidatesQuery(alertId, limit), cancellationToken);

        return Ok(candidates.Select(SpecialistResourceFromEntityAssembler.ToResourceFromDto));
    }
}
