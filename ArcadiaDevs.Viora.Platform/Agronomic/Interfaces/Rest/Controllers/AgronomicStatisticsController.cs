using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
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
///     Agronomic statistics REST controller.
/// </summary>
[ApiController]
[Route("api/v1/agronomic-statistics")]
[Produces("application/json")]
[Authorize]
public class AgronomicStatisticsController(
    IAgronomicStatisticsQueryService agronomicStatisticsQueryService,
    IAgronomicStatisticSeriesQueryService agronomicStatisticSeriesQueryService,
    IAgronomicStatisticIngestionService agronomicStatisticIngestionService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(AgronomicStatisticResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

        var result = await agronomicStatisticsQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
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
            });
    }

    [HttpGet("series")]
    [ProducesResponseType(typeof(AgronomicStatisticSeriesResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

        var result = await agronomicStatisticSeriesQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            series =>
            {
                if (series.Labels == null || series.Labels.Count == 0)
                {
                    return Ok("");
                }
                return Ok(series);
            });
    }

    [HttpPost("ingest")]
    [ProducesResponseType(typeof(AgronomicStatisticsIngestionReportResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestAgronomicStatistics(
        [FromQuery] long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new IngestAgronomicStatisticsCommand(userId, DateTimeOffset.UtcNow);
        var result = await agronomicStatisticIngestionService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            report => Ok(new AgronomicStatisticsIngestionReportResource(report.Ingested, report.Skipped)));
    }
}
