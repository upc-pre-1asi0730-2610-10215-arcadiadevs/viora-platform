using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InterventionExecutionsController(
    IInterventionExecutionCommandService interventionExecutionCommandService,
    IInterventionExecutionQueryService interventionExecutionQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(InterventionExecutionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CertifyInterventionExecution(
        [FromBody] CreateInterventionExecutionResource resource,
        CancellationToken cancellationToken = default)
    {
        var command = new CertifyInterventionExecutionCommand(
            resource.TreatmentPrescriptionId,
            resource.ApplicationDate,
            resource.AppliedArea,
            resource.ExecutionStatus,
            resource.FieldNote);

        var result = await interventionExecutionCommandService.Handle(command, cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Created(
                $"/api/v1/intervention-executions/{entity.Id}",
                InterventionExecutionResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    [HttpGet]
    [ProducesResponseType(typeof(InterventionExecutionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterventionExecutionByTreatmentPrescriptionId(
        [FromQuery] int treatmentPrescriptionId,
        CancellationToken cancellationToken = default)
    {
        var execution = await interventionExecutionQueryService.Handle(
            new GetInterventionExecutionByTreatmentPrescriptionIdQuery(treatmentPrescriptionId), cancellationToken);

        if (execution is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                InterventionErrors.NotFound.Code,
                InterventionErrors.NotFound.Message));
        }

        return Ok(InterventionExecutionResourceFromEntityAssembler.ToResourceFromEntity(execution));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InterventionExecutionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterventionExecutionById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var execution = await interventionExecutionQueryService.Handle(
            new GetInterventionExecutionByIdQuery(id), cancellationToken);

        if (execution is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                InterventionErrors.NotFound.Code,
                InterventionErrors.NotFound.Message));
        }

        return Ok(InterventionExecutionResourceFromEntityAssembler.ToResourceFromEntity(execution));
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
