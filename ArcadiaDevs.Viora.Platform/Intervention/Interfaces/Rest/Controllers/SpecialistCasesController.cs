using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for the signed-in specialist's own cases, powering
///     My Requests and Field Inspection. The specialist is derived from the
///     bearer token.
/// </summary>
[ApiController]
[Route("api/v1/specialist-cases")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistCasesController(
    ISpecialistCasesQueryService specialistCasesQueryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(SpecialistCasesResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCases(
        [FromToken] int specialistId,
        CancellationToken cancellationToken = default)
    {
        var resource = await specialistCasesQueryService.Handle(
            new GetSpecialistCasesQuery(specialistId), cancellationToken);

        return Ok(resource);
    }
}
