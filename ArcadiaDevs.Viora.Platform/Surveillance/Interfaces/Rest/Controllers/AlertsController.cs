using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
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
public class AlertsController(
    IAlertQueryService alertQueryService,
    IAlertCommandService alertCommandService) : ControllerBase
{
    /// <summary>
    ///     Get alert details
    /// </summary>
    /// <remarks>
    ///     Retrieves the complete data of an alert, including its supporting metrics and timeline history.
    /// </remarks>
    /// <param name="alertId">The ID of the alert.</param>
    /// <response code="200">Alert found</response>
    /// <response code="404">Alert not found</response>
    [HttpGet("{alertId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertById([FromRoute] long alertId)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null)
        {
            return NotFound(new EmptyResource());
        }

        var resource = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return Ok(resource);
    }

    /// <summary>
    ///     Get alert timeline
    /// </summary>
    /// <remarks>
    ///     Retrieves only the historical timeline records of an alert.
    /// </remarks>
    /// <param name="alertId">The ID of the alert.</param>
    /// <response code="200">Timeline retrieved</response>
    /// <response code="404">Alert not found</response>
    [HttpGet("{alertId:long}/timeline")]
    [ProducesResponseType(typeof(IEnumerable<AlertTimelineRecordResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertTimelineById([FromRoute] long alertId)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null)
        {
            return NotFound(new EmptyResource());
        }

        var resources = alert.Timeline.Select(t => new AlertTimelineRecordResource(
            t.Tag,
            t.Title,
            t.Description,
            t.CreatedAt
        )).ToList();

        return Ok(resources);
    }

    /// <summary>
    ///     Mark alert as reviewed
    /// </summary>
    /// <remarks>
    ///     Updates the status of an alert to UNDER_REVIEW and appends a record to its timeline.
    /// </remarks>
    /// <param name="alertId">The ID of the alert.</param>
    /// <response code="200">Alert marked as reviewed</response>
    /// <response code="400">Alert is already reviewed</response>
    /// <response code="404">Alert not found</response>
    [HttpPatch("{alertId:long}/reviewed")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(EmptyResource), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAlertAsReviewed([FromRoute] long alertId)
    {
        var command = new MarkAlertAsReviewedCommand(alertId);
        var result = await alertCommandService.Handle(command);

        if (result is Result<long, Error>.Failure failure)
        {
            if (failure.Error?.Code == "NotFound")
            {
                return NotFound(new EmptyResource());
            }
            return BadRequest(new EmptyResource());
        }

        var successId = ((Result<long, Error>.Success)result).Value;

        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(successId));
        if (alert is null) return NotFound(new EmptyResource());

        var resource = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return Ok(resource);
    }
}
