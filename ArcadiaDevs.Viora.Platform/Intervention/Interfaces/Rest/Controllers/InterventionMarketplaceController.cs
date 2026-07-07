using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for the specialist Intervention Marketplace read
///     model. The signed-in specialist is derived from the bearer token, so
///     no id travels in the URL.
/// </summary>
[ApiController]
[Route("api/v1/intervention-marketplace")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InterventionMarketplaceController(
    ISpecialistMarketplaceQueryService specialistMarketplaceQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SpecialistMarketplaceResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMarketplace(
        [FromToken] int specialistId,
        CancellationToken cancellationToken = default)
    {
        var resource = await specialistMarketplaceQueryService.Handle(
            new GetSpecialistMarketplaceQuery(specialistId), cancellationToken);

        return Ok(resource);
    }
}
