using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest;

/// <summary>
///     REST controller for agronomic statistics.
/// </summary>
[ApiController]
[Route("api/v1/agronomic-statistics")]
public class AgronomicStatisticsController(IAgronomicStatisticsQueryService agronomicStatisticsQueryService) : ControllerBase
{
    /// <summary>
    ///     Returns time series data for NDVI and cold portions for a user's plots.
    /// </summary>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="plotId">Optional plot identifier (query parameter).</param>
    /// <param name="timeRange">The time range (week, month, quarter, year).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>200 OK with list of agronomic statistics, 403 Forbidden if plot access denied, or 400 Bad Request with error details.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgronomicStatisticsResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] int userId,
        [FromQuery] int? plotId,
        [FromQuery] string timeRange,
        CancellationToken cancellationToken)
    {
        var query = new GetAgronomicStatisticsQuery(userId, plotId, timeRange);
        var result = await agronomicStatisticsQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            statistics => Ok(statistics),
            error => error.Code switch
            {
                "PLOT_ACCESS_DENIED" => StatusCode(StatusCodes.Status403Forbidden, error),
                "INVALID_TIME_RANGE" => BadRequest(error),
                _ => BadRequest(error)
            });
    }
}