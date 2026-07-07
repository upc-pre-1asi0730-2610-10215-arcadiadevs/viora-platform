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
    ///     Returns aggregated KPI metrics for the caller (REQ parity with OS:
    ///     root GET, not a dedicated <c>/current</c> sub-route). <paramref name="limit"/>
    ///     is accepted for parity with OS's own placeholder param but is not
    ///     yet used server-side — OS doesn't use it either today, it's reserved
    ///     for future pagination/history.
    /// </summary>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="limit">Reserved for future pagination/history; unused today (matches OS).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 OK with the monitoring summary, or 400 Bad Request with error details.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(MonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrent(
        [FromToken] int userId,
        [FromQuery] int limit = 1,
        CancellationToken cancellationToken = default)
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

    /// <summary>
    ///     Legacy alias for <see cref="GetCurrent"/>. Kept so existing clients
    ///     hitting the old dedicated sub-route keep working without a
    ///     coordinated frontend change; new clients should use the root route.
    /// </summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(MonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetCurrentLegacy(
        [FromToken] int userId,
        [FromQuery] int limit = 1,
        CancellationToken cancellationToken = default) =>
        GetCurrent(userId, limit, cancellationToken);
}
