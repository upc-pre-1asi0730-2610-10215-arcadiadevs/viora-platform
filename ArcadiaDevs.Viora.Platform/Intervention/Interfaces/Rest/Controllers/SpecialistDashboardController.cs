using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for the specialist segment dashboard (Overview).
///     Serves the aggregated read model for the signed-in specialist,
///     derived from the bearer token.
/// </summary>
[ApiController]
[Route("api/v1/specialist-dashboard")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistDashboardController(
    ISpecialistDashboardQueryService specialistDashboardQueryService) : ControllerBase
{
    /// <summary>
    ///     Gets the specialist dashboard read model (KPIs, zonal radar,
    ///     incoming requests, and the accepted-cases performance series).
    /// </summary>
    /// <param name="specialistId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Dashboard resource returned.</response>
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
