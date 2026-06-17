using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using ArcadiaDevs.Viora.Platform.Surveillance.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/pest-sighting-reports")]
[Produces(MediaTypeNames.Application.Json)]
public class PestSightingReportsController(
    IPestSightingCommandService commandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreatePestSightingReportResource resource)
    {
        var command = CreatePestSightingReportCommandFromResourceAssembler.ToCommandFromResource(resource);
        
        var result = await commandService.Handle(command);

        return SurveillanceActionResultAssembler.ToActionResultFromCreatePestSightingReportResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            aggregate => StatusCode(201, PestSightingReportResourceFromEntityAssembler.ToResourceFromEntity(aggregate))
        );
    }
}
