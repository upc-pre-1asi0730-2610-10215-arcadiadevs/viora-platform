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

/// <summary>
///     REST controller for intervention outcomes (REQ-IO-1..4).
/// </summary>
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
    /// <summary>
    ///     Reports the impact of an intervention execution, creating a new outcome.
    /// </summary>
    /// <remarks>
    ///     Validates <c>interventionExecutionId</c> exists (REQ-CC-2:
    ///     missing FK maps to 404). Fails 409 if an outcome already exists
    ///     for the given execution (REQ-IO-3 idempotency).
    /// </remarks>
    /// <param name="resource">The impact report payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Outcome created (status <c>IMPACT_REPORTED</c>).</response>
    /// <response code="400">Invalid field values.</response>
    /// <response code="404">The referenced interventionExecutionId does not exist.</response>
    /// <response code="409">An outcome already exists for the given interventionExecutionId.</response>
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

    /// <summary>
    ///     Finds the (at most one) outcome linked to an intervention execution
    ///     (REQ-IO-3 idempotency lookup, reused as a read endpoint).
    /// </summary>
    /// <param name="interventionExecutionId">The intervention execution id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Outcome found.</response>
    /// <response code="404">No outcome exists for the given interventionExecutionId.</response>
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

    /// <summary>
    ///     Gets an intervention outcome by id.
    /// </summary>
    /// <param name="id">The outcome id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Outcome found.</response>
    /// <response code="404">Outcome not found.</response>
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

    /// <summary>
    ///     Closes the outcome with the grower's service evaluation.
    /// </summary>
    /// <remarks>
    ///     Self-guarded on the aggregate — only succeeds if the outcome is
    ///     not already <c>CLOSED</c> (REQ-IO-2, 409 otherwise).
    /// </remarks>
    /// <param name="id">The outcome id.</param>
    /// <param name="resource">The evaluation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Outcome closed.</response>
    /// <response code="400">Invalid field values.</response>
    /// <response code="404">Outcome not found.</response>
    /// <response code="409">The outcome is already <c>CLOSED</c>.</response>
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
