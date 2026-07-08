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
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertSummaryResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromToken] long userId,
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

    [HttpGet("{alertId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertById(
        [FromRoute] long alertId,
        [FromToken] long userId,
        [FromQuery] string? view = null)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId, userId));
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
