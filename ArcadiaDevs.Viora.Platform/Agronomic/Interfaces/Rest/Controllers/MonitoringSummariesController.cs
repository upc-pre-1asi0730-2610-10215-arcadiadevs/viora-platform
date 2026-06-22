using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for monitoring summaries.
/// </summary>
[ApiController]
[Route("api/v1/monitoring-summaries")]
public class MonitoringSummariesController(IMonitoringSummaryQueryService monitoringSummaryQueryService) : ControllerBase
{
    /// <summary>
    ///     Returns aggregated KPI metrics for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="authenticatedUserId">The authenticated user identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 OK with the monitoring summary, or 400 Bad Request with error details.</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(MonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrent(
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetCurrentMonitoringSummaryQuery(userId);
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            summary => Ok(summary),
            error => BadRequest(error));
    }
}