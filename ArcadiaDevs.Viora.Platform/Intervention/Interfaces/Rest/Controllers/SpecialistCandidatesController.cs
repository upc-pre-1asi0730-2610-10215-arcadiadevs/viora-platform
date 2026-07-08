using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistCandidatesController(
    ISpecialistQueryService specialistQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    public const int DefaultCandidateLimit = 3;

    public const int MaxCandidateLimit = 50;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpecialistResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSpecialistCandidates(
        [FromQuery] long? alertId,
        [FromQuery] int limit = DefaultCandidateLimit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                InterventionErrors.ValidationError.Code,
                errorLocalizer[InterventionErrors.ValidationError.Code].Value ?? InterventionErrors.ValidationError.Message);
            return BadRequest(problemDetails);
        }

        var effectiveLimit = Math.Min(limit, MaxCandidateLimit);

        var candidates = await specialistQueryService.Handle(
            new GetSpecialistCandidatesQuery(alertId, effectiveLimit), cancellationToken);

        return Ok(candidates.Select(SpecialistResourceFromDtoAssembler.ToResourceFromDto));
    }
}
