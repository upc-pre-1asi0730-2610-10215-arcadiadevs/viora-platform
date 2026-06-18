using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest;

/// <summary>
///     Agronomic statistics REST controller.
/// </summary>
[ApiController]
[Route("api/v1/agronomic-statistics")]
[Produces("application/json")]
public class AgronomicStatisticsController : ControllerBase
{
    private readonly IAgronomicStatisticsQueryService _agronomicStatisticsQueryService;
    private readonly IAgronomicStatisticSeriesQueryService _agronomicStatisticSeriesQueryService;
    private readonly IAgronomicStatisticIngestionService _agronomicStatisticIngestionService;

    public AgronomicStatisticsController(
        IAgronomicStatisticsQueryService agronomicStatisticsQueryService,
        IAgronomicStatisticSeriesQueryService agronomicStatisticSeriesQueryService,
        IAgronomicStatisticIngestionService agronomicStatisticIngestionService)
    {
        _agronomicStatisticsQueryService = agronomicStatisticsQueryService;
        _agronomicStatisticSeriesQueryService = agronomicStatisticSeriesQueryService;
        _agronomicStatisticIngestionService = agronomicStatisticIngestionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AgronomicStatisticResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAgronomicStatistics(
        [FromQuery] long userId,
        [FromQuery] string timeRange,
        [FromQuery] long? plotId = null,
        [FromHeader(Name = "X-Authenticated-User-Id")] long? authenticatedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveAuthenticatedUserId = authenticatedUserId ?? userId;
        
        var query = new GetAgronomicStatisticsQuery(
            userId,
            effectiveAuthenticatedUserId,
            plotId,
            Enum.Parse<ETimeRange>(timeRange, ignoreCase: true)
        );

        var result = await _agronomicStatisticsQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            statistics => 
            {
                var latest = statistics.OrderByDescending(s => s.MeasurementDate).FirstOrDefault();
                if (latest == null)
                {
                    return Ok("");
                }
                
                return Ok(new AgronomicStatisticResource(
                    latest.MeasurementDate.ToString("yyyy-MM-dd"),
                    latest.NdviValue,
                    latest.ChillPortions,
                    latest.ChillHours
                ));
            },
            error => error.Code switch
            {
                "PLOT_OWNERSHIP" or "AGRONOMIC_STATISTICS_ACCESS" => StatusCode(StatusCodes.Status403Forbidden, error),
                _ => BadRequest(error)
            });
    }

    [HttpGet("series")]
    [ProducesResponseType(typeof(AgronomicStatisticSeriesResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAgronomicStatisticSeries(
        [FromQuery] long userId,
        [FromQuery] string timeRange,
        [FromQuery] long? plotId = null,
        [FromHeader(Name = "X-Authenticated-User-Id")] long? authenticatedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveAuthenticatedUserId = authenticatedUserId ?? userId;

        var query = new GetAgronomicStatisticSeriesQuery(
            userId,
            effectiveAuthenticatedUserId,
            plotId,
            Enum.Parse<ETimeRange>(timeRange, ignoreCase: true)
        );

        var result = await _agronomicStatisticSeriesQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            series =>
            {
                if (series.Labels == null || series.Labels.Count == 0)
                {
                    return Ok("");
                }
                return Ok(series);
            },
            error => error.Code switch
            {
                "PLOT_OWNERSHIP" or "AGRONOMIC_STATISTICS_ACCESS" => StatusCode(StatusCodes.Status403Forbidden, error),
                _ => BadRequest(error)
            });
    }

    [HttpPost("ingest")]
    [ProducesResponseType(typeof(AgronomicStatisticsIngestionReportResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestAgronomicStatistics(
        [FromQuery] long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new IngestAgronomicStatisticsCommand(userId, DateTimeOffset.UtcNow);
        var result = await _agronomicStatisticIngestionService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            report => Ok(new AgronomicStatisticsIngestionReportResource(report.Ingested, report.Skipped)),
            BadRequest
        );
    }
}