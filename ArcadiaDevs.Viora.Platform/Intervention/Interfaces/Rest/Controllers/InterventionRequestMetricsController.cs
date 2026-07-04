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

/// <summary>
///     REST controller for aggregate intervention-request metrics (REQ-OV-3).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InterventionRequestMetricsController(
    IInterventionRequestMetricsQueryService interventionRequestMetricsQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Gets aggregate request metrics scoped to a grower or a specialist.
    /// </summary>
    /// <remarks>
    ///     Exactly one of <paramref name="growerId" />/<paramref name="specialistId" />
    ///     is expected; if both are omitted, 400 is returned. If both are
    ///     supplied, <paramref name="growerId" /> takes precedence.
    /// </remarks>
    /// <param name="growerId">Scope to a single grower.</param>
    /// <param name="specialistId">Scope to a single specialist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Metrics returned.</response>
    /// <response code="400">Neither <c>growerId</c> nor <c>specialistId</c> was supplied.</response>
    [HttpGet]
    [ProducesResponseType(typeof(InterventionRequestMetricsResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInterventionRequestMetrics(
        [FromQuery] int? growerId,
        [FromQuery] int? specialistId,
        CancellationToken cancellationToken = default)
    {
        if (growerId is null && specialistId is null)
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                InterventionErrors.ValidationError.Code,
                errorLocalizer[InterventionErrors.ValidationError.Code].Value ?? InterventionErrors.ValidationError.Message);
            return BadRequest(problemDetails);
        }

        var metrics = await interventionRequestMetricsQueryService.Handle(
            new GetInterventionRequestMetricsQuery(growerId, specialistId), cancellationToken);

        return Ok(InterventionRequestMetricsResourceFromDtoAssembler.ToResourceFromDto(metrics));
    }
}
