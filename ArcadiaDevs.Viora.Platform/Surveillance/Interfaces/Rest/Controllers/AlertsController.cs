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
    IAlertCommandService alertCommandService) : ControllerBase
{
    /// <summary>
    ///     Get alerts
    /// </summary>
    /// <remarks>
    ///     Retrieves alerts for the given user. Use <c>?sort=recent</c> to get the most
    ///     recent alerts matching the dashboard overview.
    /// </remarks>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="sort">Sorting criteria (e.g. <c>recent</c>).</param>
    /// <param name="limit">The maximum number of alerts to return.</param>
    /// <response code="200">Alerts retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertSummaryResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] long userId,
        [FromQuery] string? sort = null,
        [FromQuery] int limit = 3)
    {
        if (string.Equals(sort, "recent", StringComparison.OrdinalIgnoreCase))
        {
            var query = new GetRecentAlertsByUserIdQuery(userId, limit);
            var summaries = await alertQueryService.Handle(query);
            return Ok(summaries);
        }

        // No specific sort/query is supported yet for the full alerts collection.
        return Ok(Enumerable.Empty<AlertSummaryResource>());
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
}
