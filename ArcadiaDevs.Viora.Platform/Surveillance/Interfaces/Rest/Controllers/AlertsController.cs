using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AlertsController(IAlertQueryService alertQueryService) : ControllerBase
{
    /// <summary>
    ///     Get alert details
    /// </summary>
    /// <param name="alertId">The ID of the alert.</param>
    /// <returns>The alert details if found.</returns>
    [HttpGet("{alertId:long}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertById([FromRoute] long alertId)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null)
        {
            return NotFound();
        }

        var resource = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return Ok(resource);
    }

    /// <summary>
    ///     Get alert timeline
    /// </summary>
    /// <param name="alertId">The ID of the alert.</param>
    /// <returns>The historical timeline records of the alert.</returns>
    [HttpGet("{alertId:long}/timeline")]
    [ProducesResponseType(typeof(IEnumerable<AlertTimelineRecordResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertTimelineById([FromRoute] long alertId)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null)
        {
            return NotFound(new { }); // Swagger expects empty object for 404 in Java example
        }

        var resources = alert.Timeline.Select(t => new AlertTimelineRecordResource(
            t.Tag,
            t.Title,
            t.Description,
            t.CreatedAt
        )).ToList();

        return Ok(resources);
    }
}
