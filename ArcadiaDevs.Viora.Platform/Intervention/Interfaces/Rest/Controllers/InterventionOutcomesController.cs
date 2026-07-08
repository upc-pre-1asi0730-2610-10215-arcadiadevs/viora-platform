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
public class InterventionOutcomesController(
    IInterventionOutcomeCommandService interventionOutcomeCommandService,
    IInterventionOutcomeQueryService interventionOutcomeQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(InterventionOutcomeResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReportImpact(
        [FromBody] CreateInterventionOutcomeResource resource,
        CancellationToken cancellationToken = default)
    {
        var command = new ReportImpactCommand(
            resource.InterventionExecutionId,
            resource.GracePeriod,
            resource.ObservedResult,
            resource.ImpactLevel,
            resource.ProducerAssessment);

        var result = await interventionOutcomeCommandService.Handle(command, cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Created(
                $"/api/v1/intervention-outcomes/{entity.Id}",
                InterventionOutcomeResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    [HttpGet]
    [ProducesResponseType(typeof(InterventionOutcomeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterventionOutcomeByInterventionExecutionId(
        [FromQuery] int interventionExecutionId,
        CancellationToken cancellationToken = default)
    {
        var outcome = await interventionOutcomeQueryService.Handle(
            new GetInterventionOutcomeByInterventionExecutionIdQuery(interventionExecutionId), cancellationToken);

        if (outcome is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                InterventionErrors.NotFound.Code,
                InterventionErrors.NotFound.Message));
        }

        return Ok(InterventionOutcomeResourceFromEntityAssembler.ToResourceFromEntity(outcome));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InterventionOutcomeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterventionOutcomeById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var outcome = await interventionOutcomeQueryService.Handle(
            new GetInterventionOutcomeByIdQuery(id), cancellationToken);

        if (outcome is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                InterventionErrors.NotFound.Code,
                InterventionErrors.NotFound.Message));
        }

        return Ok(InterventionOutcomeResourceFromEntityAssembler.ToResourceFromEntity(outcome));
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(InterventionOutcomeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseOutcomeWithEvaluation(
        [FromRoute] int id,
        [FromBody] UpdateInterventionOutcomeResource resource,
        CancellationToken cancellationToken = default)
    {
        var command = new CloseOutcomeWithEvaluationCommand(
            id,
            resource.ServiceResult,
            resource.HireAgain,
            resource.PrivateFeedback);

        var result = await interventionOutcomeCommandService.Handle(command, cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Ok(InterventionOutcomeResourceFromEntityAssembler.ToResourceFromEntity(entity)));
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
