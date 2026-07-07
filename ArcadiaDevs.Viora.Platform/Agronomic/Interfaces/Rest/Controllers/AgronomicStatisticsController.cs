using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
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
    ProblemDetailsFactory problemDetailsFactory,
    IClock clock) : ControllerBase
{
    /// <summary>
    ///     Gets agronomic statistics for the caller, or the chart-series shape
    ///     when <c>?view=series</c> is given (REQ parity with OS: a single root
    ///     GET disambiguated by a <c>view</c> query param, not a dedicated
    ///     <c>/series</c> sub-route).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AgronomicStatisticResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AgronomicStatisticSeriesResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAgronomicStatistics(
        [FromToken] long userId,
        [FromQuery] string timeRange,
        [FromQuery] long? plotId = null,
        [FromQuery] string? view = null,
        CancellationToken cancellationToken = default)
    {
        // Validate explicitly instead of Enum.Parse: an unrecognized timeRange value
        // used to throw an unhandled ArgumentException that GlobalExceptionHandlerMiddleware's
        // catch-all mapped to 500. Mirrors the invalid-?view= handling in PlotsController.
        if (!Enum.TryParse<ETimeRange>(timeRange, ignoreCase: true, out var parsedTimeRange))
        {
            return BadRequest(new
            {
                error = $"Invalid timeRange '{timeRange}'. Valid values: {string.Join(", ", Enum.GetNames<ETimeRange>())}."
            });
        }

        if (string.Equals(view, "series", StringComparison.OrdinalIgnoreCase))
        {
            var seriesQuery = new GetAgronomicStatisticSeriesQuery(userId, userId, plotId, parsedTimeRange);
            var seriesResult = await agronomicStatisticSeriesQueryService.Handle(seriesQuery, cancellationToken);

            return AgronomicActionResultAssembler.ToActionResult(
                this,
                seriesResult,
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

        var query = new GetAgronomicStatisticsQuery(
            userId,
            userId,
            plotId,
            parsedTimeRange
        );

        var result = await agronomicStatisticsQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            statistics =>
            {
                var resources = statistics
                    .OrderBy(s => s.MeasurementDate)
                    .Select(s => new AgronomicStatisticResource(
                        s.MeasurementDate.ToString("yyyy-MM-dd"),
                        s.NdviValue,
                        s.ChillPortions,
                        s.ChillHours))
                    .ToList();

                return Ok(resources);
            });
    }

    /// <summary>
    ///     Legacy alias for <see cref="GetAgronomicStatistics"/> with <c>view=series</c>
    ///     implied. Kept so existing clients hitting the old dedicated sub-route
    ///     keep working without a coordinated frontend change; new clients should
    ///     use <c>?view=series</c> on the root route instead.
    /// </summary>
    [HttpGet("series")]
    [ProducesResponseType(typeof(AgronomicStatisticSeriesResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAgronomicStatisticSeriesLegacy(
        [FromToken] long userId,
        [FromQuery] string timeRange,
        [FromQuery] long? plotId = null,
        CancellationToken cancellationToken = default) =>
        GetAgronomicStatistics(userId, timeRange, plotId, view: "series", cancellationToken);

    [HttpPost]
    [ProducesResponseType(typeof(AgronomicStatisticsIngestionReportResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestAgronomicStatistics(
        [FromToken] long userId,
        CancellationToken cancellationToken = default)
    {
        var command = new IngestAgronomicStatisticsCommand(userId, new DateTimeOffset(clock.UtcNow, TimeSpan.Zero));
        var result = await agronomicStatisticIngestionService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            report => Ok(new AgronomicStatisticsIngestionReportResource(report.Ingested, report.Skipped)));
    }
}
