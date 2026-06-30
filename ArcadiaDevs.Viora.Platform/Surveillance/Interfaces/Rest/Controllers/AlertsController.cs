using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

/// <summary>
/// An empty resource to represent an empty JSON object {}
/// </summary>
public record EmptyResource();

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class AlertsController(
    IAlertQueryService alertQueryService,
    IAlertCommandService alertCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Get alerts
    /// </summary>
    /// <remarks>
    ///     Retrieves alerts for the given user. Supported sort keys:
    ///     <c>?sort=recent</c> (most recent first, default),
    ///     <c>?sort=severity</c> (highest severity first),
    ///     <c>?sort=type</c> (alphabetical by threat type).
    ///     Any unknown sort key falls back to <c>recent</c>.
    ///     Returns <c>[]</c> (200) on an empty timeline, not 500.
    /// </remarks>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="sort">Sorting criteria (e.g. <c>recent</c>, <c>severity</c>, <c>type</c>).</param>
    /// <param name="limit">The maximum number of alerts to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Alerts retrieved successfully (or empty list)</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertSummaryResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] long userId,
        [FromQuery] string? sort = null,
        [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        // SURV-003 sort-placeholder fix: route every supported sort key
        // through IAlertQueryService so the controller no longer returns
        // an empty list for any sort other than "recent". The query
        // service normalises the sort key (null/unknown -> "recent") and
        // returns an empty list (never null) for empty timelines.
        var query = new GetAlertsByUserIdQuery(
            UserId: userId,
            Sort: sort ?? "recent",
            Limit: limit);

        var summaries = await alertQueryService.Handle(query, cancellationToken);
        return Ok(summaries ?? Enumerable.Empty<AlertSummaryResource>());
    }

    /// <summary>
    ///     Get alert details or timeline
    /// </summary>
    /// <remarks>
    ///     Retrieves the complete data of an alert. Use <c>?view=timeline</c> to get only
    ///     the historical timeline records.
    /// </remarks>
    /// <param name="alertId">The ID of the alert.</param>
    /// <param name="view">Projection view. Supported values: <c>timeline</c>.</param>
    /// <response code="200">Alert found or timeline retrieved</response>
    /// <response code="404">Alert not found</response>
    [HttpGet("{alertId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertById(
        [FromRoute] long alertId,
        [FromQuery] string? view = null)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null)
        {
            return NotFound(new EmptyResource());
        }

        if (string.Equals(view, "timeline", StringComparison.OrdinalIgnoreCase))
        {
            var records = alert.Timeline.Select(t => new AlertTimelineRecordResource(
                t.Tag,
                t.Title,
                t.Description,
                t.CreatedAt
            )).ToList();

            return Ok(records);
        }

        var resource = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return Ok(resource);
    }

    /// <summary>
    ///     Update alert status
    /// </summary>
    /// <remarks>
    ///     Partially updates an alert. Pass <c>{"status": "UNDER_REVIEW"}</c> to mark it
    ///     as reviewed and append a record to its timeline.
    /// </remarks>
    /// <param name="alertId">The ID of the alert.</param>
    /// <param name="resource">The update payload.</param>
    /// <response code="200">Alert updated successfully</response>
    /// <response code="400">Alert is already reviewed or invalid status</response>
    /// <response code="404">Alert not found</response>
    [HttpPatch("{alertId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAlert(
        [FromRoute] long alertId,
        [FromBody] UpdateAlertResource resource)
    {
        if (!string.Equals(resource.Status, "UNDER_REVIEW", StringComparison.OrdinalIgnoreCase))
        {
            // Only the transition to UNDER_REVIEW is supported for now.
            return BadRequest(new EmptyResource());
        }

        var command = new MarkAlertAsReviewedCommand(alertId);
        var result = await alertCommandService.Handle(command);

        if (result is Result<long, Error>.Failure failure)
        {
            if (failure.Error?.Code == SurveillanceErrors.NotFound.Code)
            {
                return NotFound(new EmptyResource());
            }
            return BadRequest(new EmptyResource());
        }

        var successId = ((Result<long, Error>.Success)result).Value;

        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(successId));
        if (alert is null) return NotFound(new EmptyResource());

        var updated = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return Ok(updated);
    }

    // ============================================================
    // SURV-003 state-transition endpoints
    //   POST /api/v1/alerts/{id}/confirm   (state machine: any non-terminal -> UNDER_REVIEW; severity +1)
    //   POST /api/v1/alerts/{id}/dismiss    (any non-DISMISSED -> DISMISSED; terminal)
    //   POST /api/v1/alerts/{id}/escalate   (severity +1; no state change)
    //   POST /api/v1/alerts/{id}/link-report (attach a PestSightingReportId; no state change)
    // Each endpoint:
    //   * is [Authorize]-protected (class-level)
    //   * loads the alert, calls the matching state-machine method
    //   * persists via IAlertCommandService (existing MarkAlertAsReviewedCommand path
    //     will be generalised in a follow-up; for now, the state machine is invoked
    //     in-process against the loaded aggregate and the result is mapped to
    //     RFC 7807 ProblemDetails on failure (CC-6)
    // ============================================================

    /// <summary>
    ///     Confirm an alert from inspection (SURV-003).
    /// </summary>
    /// <param name="alertId">The alert id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Alert confirmed; status <c>UNDER_REVIEW</c>, severity raised one level.</response>
    /// <response code="400">Alert is in a terminal state (RFC 7807 ProblemDetails).</response>
    /// <response code="404">Alert not found.</response>
    [HttpPost("{alertId:long}/confirm")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm(
        [FromRoute] long alertId,
        CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new ConfirmAlertCommand(alertId), cancellationToken);
        if (result.IsFailure)
        {
            return MapTransitionFailureToResult(result);
        }
        return await BuildOkWithAlertAsync(alertId, cancellationToken);
    }

    /// <summary>
    ///     Dismiss an alert (SURV-003).
    /// </summary>
    /// <param name="alertId">The alert id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Alert dismissed.</response>
    /// <response code="400">Alert is already dismissed (RFC 7807 ProblemDetails).</response>
    /// <response code="404">Alert not found.</response>
    [HttpPost("{alertId:long}/dismiss")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Dismiss(
        [FromRoute] long alertId,
        CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new DismissAlertCommand(alertId), cancellationToken);
        if (result.IsFailure)
        {
            return MapTransitionFailureToResult(result);
        }
        return await BuildOkWithAlertAsync(alertId, cancellationToken);
    }

    /// <summary>
    ///     Escalate an alert's severity (SURV-003).
    /// </summary>
    /// <param name="alertId">The alert id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Alert severity raised one level.</response>
    /// <response code="404">Alert not found.</response>
    [HttpPost("{alertId:long}/escalate")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Escalate(
        [FromRoute] long alertId,
        CancellationToken cancellationToken)
    {
        var result = await alertCommandService.Handle(new EscalateAlertCommand(alertId), cancellationToken);
        if (result.IsFailure)
        {
            return MapTransitionFailureToResult(result);
        }
        return await BuildOkWithAlertAsync(alertId, cancellationToken);
    }

    /// <summary>
    ///     Link a pest sighting report to an alert (SURV-003).
    /// </summary>
    /// <param name="alertId">The alert id.</param>
    /// <param name="reportId">The pest sighting report id to attach.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Report linked; no state change.</response>
    /// <response code="404">Alert not found.</response>
    [HttpPost("{alertId:long}/link-report")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkReport(
        [FromRoute] long alertId,
        [FromQuery] long reportId,
        CancellationToken cancellationToken = default)
    {
        var result = await alertCommandService.Handle(new LinkAlertReportCommand(alertId, reportId), cancellationToken);
        if (result.IsFailure)
        {
            return MapTransitionFailureToResult(result);
        }
        return await BuildOkWithAlertAsync(alertId, cancellationToken);
    }

    // ----------------------------------------------------------------
    // shared helpers: load alert, map Result<Unit,Error> -> 4xx
    // ProblemDetails (CC-6).
    // ----------------------------------------------------------------
    private IActionResult MapTransitionFailureToResult(Result<Unit, Error> failure)
    {
        var failureCase = (Result<Unit, Error>.Failure)failure;
        var statusCode = failureCase.Error.Code == SurveillanceErrors.NotFound.Code
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return StatusCode(
            statusCode,
            BuildProblemDetails(statusCode, failureCase.Error.Code, failureCase.Error.Message));
    }

    private async Task<IActionResult> BuildOkWithAlertAsync(long alertId, CancellationToken cancellationToken)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId), cancellationToken);
        if (alert is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                SurveillanceErrors.NotFound.Code,
                SurveillanceErrors.NotFound.Message));
        }
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }

    private ProblemDetails BuildProblemDetails(int statusCode, string code, string message)
    {
        return problemDetailsFactory.CreateProblemDetails(
            HttpContext,
            statusCode,
            code,
            errorLocalizer[code].Value ?? message);
    }
}
