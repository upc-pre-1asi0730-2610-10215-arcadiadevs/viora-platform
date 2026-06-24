using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for the anonymized Community Risk snapshot.
/// </summary>
[ApiController]
[Route("api/v1/community-risk")]
[Produces(MediaTypeNames.Application.Json)]
public class CommunityRiskController(ICommunityRiskQueryService communityRiskQueryService) : ControllerBase
{
    /// <summary>
    ///     Get community risk around a plot
    /// </summary>
    /// <remarks>
    ///     Returns anonymized nearby risk signals (derived from active alerts on neighbor plots
    ///     within the radius) plus preventive recommendations.
    /// </remarks>
    /// <param name="plotId">The reference plot identifier.</param>
    /// <param name="radiusKm">The monitoring radius in kilometers (defaults to 10).</param>
    /// <response code="200">Community risk snapshot retrieved</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Reference plot not found</response>
    [HttpGet]
    [ProducesResponseType(typeof(CommunityRiskResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCommunityRisk(
        [FromQuery] long plotId,
        [FromQuery] double radiusKm = 10)
    {
        if (plotId <= 0 || radiusKm <= 0)
        {
            return BadRequest(new EmptyResource());
        }

        var query = new GetCommunityRiskByPlotQuery(plotId, radiusKm);
        var snapshot = await communityRiskQueryService.Handle(query);

        if (snapshot is null)
        {
            return NotFound(new EmptyResource());
        }

        return Ok(snapshot);
    }
}
