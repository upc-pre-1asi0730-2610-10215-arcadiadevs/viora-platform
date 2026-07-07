using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for monitoring summaries.
/// </summary>
[ApiController]
[Route("api/v1/monitoring-summaries")]
[Authorize]
public class MonitoringSummariesController(
    IMonitoringSummaryQueryService monitoringSummaryQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Returns aggregated KPI metrics for a specific user.
    /// </summary>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 OK with the monitoring summary, or 400 Bad Request with error details.</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(MonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrent(
        [FromToken] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentMonitoringSummaryQuery(userId);
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            summary => Ok(summary));
    }
}
