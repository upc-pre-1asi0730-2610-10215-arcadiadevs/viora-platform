using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/community-risk")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class CommunityRiskController(ICommunityRiskQueryService communityRiskQueryService) : ControllerBase
{
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
