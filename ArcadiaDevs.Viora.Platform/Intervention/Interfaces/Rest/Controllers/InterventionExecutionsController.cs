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
///     REST controller for intervention executions (REQ-IE-1..3).
/// </summary>
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
    /// <summary>
    ///     Certifies a new intervention execution for a prescribed treatment.
    /// </summary>
    /// <remarks>
    ///     Fails 409 unless <c>treatmentPrescriptionId</c> resolves to a
    ///     prescription in <c>PRESCRIBED</c> status (REQ-IE-1). Fails 409 if
    ///     an execution already exists for the given prescription (REQ-IE-2
    ///     idempotency).
    /// </remarks>
    /// <param name="resource">The certification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Execution certified.</response>
    /// <response code="400">Invalid field values (e.g. unrecognized <c>executionStatus</c>).</response>
    /// <response code="404">The referenced treatmentPrescriptionId does not exist.</response>
    /// <response code="409">The prescription is not PRESCRIBED, or an execution already exists for it.</response>
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

    /// <summary>
    ///     Finds the (at most one) execution linked to a treatment prescription
    ///     (REQ-IE-2 idempotency lookup, reused as a read endpoint).
    /// </summary>
    /// <param name="treatmentPrescriptionId">The treatment prescription id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Execution found.</response>
    /// <response code="404">No execution exists for the given treatmentPrescriptionId.</response>
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

    /// <summary>
    ///     Gets an intervention execution by id.
    /// </summary>
    /// <param name="id">The execution id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Execution found.</response>
    /// <response code="404">Execution not found.</response>
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
