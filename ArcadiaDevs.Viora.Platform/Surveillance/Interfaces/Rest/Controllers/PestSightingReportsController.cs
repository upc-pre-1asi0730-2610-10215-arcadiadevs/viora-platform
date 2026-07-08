using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/pest-sighting-reports")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class PestSightingReportsController(
    IPestSightingCommandService commandService,
    IPestSightingReportQueryService queryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PestSightingReportResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReport(
        [FromBody] CreatePestSightingReportResource resource,
        [FromToken] long reporterUserId)
    {
        var command = CreatePestSightingReportCommandFromResourceAssembler.ToCommandFromResource(resource, reporterUserId);
        
        var result = await commandService.Handle(command);

        return SurveillanceActionResultAssembler.ToActionResultFromCreatePestSightingReportResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            aggregate => StatusCode(201, PestSightingReportResourceFromEntityAssembler.ToResourceFromEntity(aggregate))
        );
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PestSightingReportResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromToken] long reporterUserId,
        CancellationToken cancellationToken = default)
    {
        var reports = await queryService.Handle(new GetPestSightingReportsByUserQuery(reporterUserId), cancellationToken);
        var resources = reports.Select(PestSightingReportResourceFromEntityAssembler.ToResourceFromEntity);
        return Ok(resources);
    }

    [HttpPatch("{reportId}")]
    [ProducesResponseType(typeof(PestSightingReportResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewPestSightingReport(
        [FromRoute] long reportId,
        [FromToken] long reporterUserId,
        [FromBody] ReviewPestSightingReportResource resource,
        CancellationToken ct = default)
    {
        var command = ReviewPestSightingReportCommandFromResourceAssembler.ToCommandFromResource(reportId, reporterUserId, resource);

        var result = await commandService.Handle(command, ct);

        return result.Fold<IActionResult>(
            aggregate => Ok(PestSightingReportResourceFromEntityAssembler.ToResourceFromEntity(aggregate)),
            error => BadRequest(new ProblemDetails { Title = error.Message }));
    }
}
