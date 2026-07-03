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
    ///     Partially updates an alert. Supported target <c>status</c> values:
    ///     <c>UNDER_REVIEW</c> (marks the alert as reviewed), <c>RESOLVED</c>
    ///     (unconditional terminal transition), and <c>DISMISSED</c> (terminal
    ///     transition; optionally pass <c>{"reason": "..."}</c> to record a
    ///     caller-supplied dismissal reason on the timeline, REQ-5). Pass
    ///     <c>{"raiseSeverity": true}</c> to raise the alert's severity by one
    ///     level: combined with <c>status: "UNDER_REVIEW"</c> this confirms
    ///     the alert from inspection (severity +1, status becomes
    ///     <c>UNDER_REVIEW</c>); with <c>status</c> omitted, it escalates
    ///     severity only, with no status change. Omitting both <c>status</c>
    ///     and <c>raiseSeverity</c> (or passing an unsupported status)
    ///     returns 400.
    /// </remarks>
    /// <param name="alertId">The ID of the alert.</param>
    /// <param name="resource">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Alert updated successfully</response>
    /// <response code="400">Nothing to do, invalid status, or the transition failed (RFC 7807 ProblemDetails when raising severity)</response>
    /// <response code="404">Alert not found</response>
    [HttpPatch("{alertId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAlert(
        [FromRoute] long alertId,
        [FromBody] UpdateAlertResource resource,
        CancellationToken cancellationToken = default)
    {
        var hasStatus = !string.IsNullOrWhiteSpace(resource.Status);

        if (hasStatus && string.Equals(resource.Status, "UNDER_REVIEW", StringComparison.OrdinalIgnoreCase))
        {
            if (resource.RaiseSeverity)
            {
                var confirmResult = await alertCommandService.Handle(new ConfirmAlertCommand(alertId), cancellationToken);
                if (confirmResult.IsFailure)
                {
                    return MapTransitionFailureToResult(confirmResult);
                }
                return await BuildOkWithAlertAsync(alertId, cancellationToken);
            }

            var reviewedResult = await alertCommandService.Handle(new MarkAlertAsReviewedCommand(alertId), cancellationToken);
            return await HandleLegacyResultAsync(reviewedResult);
        }

        if (!hasStatus && resource.RaiseSeverity)
        {
            var escalateResult = await alertCommandService.Handle(new EscalateAlertCommand(alertId), cancellationToken);
            if (escalateResult.IsFailure)
            {
                return MapTransitionFailureToResult(escalateResult);
            }
            return await BuildOkWithAlertAsync(alertId, cancellationToken);
        }

        if (hasStatus && string.Equals(resource.Status, "RESOLVED", StringComparison.OrdinalIgnoreCase))
        {
            var unitResult = await alertCommandService.Handle(new ResolveAlertCommand(alertId), cancellationToken);
            return await HandleLegacyResultAsync(unitResult.Map(_ => alertId));
        }

        if (hasStatus && string.Equals(resource.Status, "DISMISSED", StringComparison.OrdinalIgnoreCase))
        {
            var unitResult = await alertCommandService.Handle(new DismissAlertCommand(alertId, resource.Reason), cancellationToken);
            return await HandleLegacyResultAsync(unitResult.Map(_ => alertId));
        }

        // Only UNDER_REVIEW, RESOLVED, DISMISSED, and raiseSeverity-only requests are supported.
        return BadRequest(new EmptyResource());
    }

    // confirm/dismiss/escalate/link-report folded into PATCH+PUT below for REST uniformity (no verb-in-URL actions)

    /// <summary>
    ///     Link a pest sighting report to an alert
    /// </summary>
    /// <remarks>
    ///     Idempotently sets the alert's linked report to <paramref name="reportId"/>;
    ///     no status or severity change (SURV-003).
    /// </remarks>
    /// <param name="alertId">The alert id.</param>
    /// <param name="reportId">The pest sighting report id to attach.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Report linked; no state change.</response>
    /// <response code="404">Alert not found.</response>
    [HttpPut("{alertId:long}/report/{reportId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkReport(
        [FromRoute] long alertId,
        [FromRoute] long reportId,
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

    /// <summary>
    ///     Maps a <see cref="Result{TValue, TError}"/> using the legacy
    ///     (pre-SURV-003) <see cref="EmptyResource"/> failure-body style used
    ///     by the original status-only PATCH branches, then loads and
    ///     returns the updated alert on success.
    /// </summary>
    private async Task<IActionResult> HandleLegacyResultAsync(Result<long, Error> result)
    {
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
