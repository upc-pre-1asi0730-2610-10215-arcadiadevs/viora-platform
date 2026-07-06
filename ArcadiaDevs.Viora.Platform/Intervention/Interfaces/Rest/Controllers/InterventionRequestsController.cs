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
///     REST controller for intervention requests (REQ-IREQ-1..4).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class InterventionRequestsController(
    IInterventionRequestCommandService interventionRequestCommandService,
    IInterventionRequestQueryService interventionRequestQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Creates a new intervention request.
    /// </summary>
    /// <remarks>
    ///     Validates <c>growerId</c> (Iam), <c>plotId</c> (Agronomic),
    ///     <c>specialistId</c> (Specialist repository), and <c>alertId</c>
    ///     (Surveillance, if provided) through their respective ACLs — a
    ///     missing FK maps to 404 (REQ-CC-2, REQ-IREQ-1).
    /// </remarks>
    /// <param name="resource">The request payload.</param>
    /// <param name="growerId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Request created (status <c>PENDING</c>).</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="404">A referenced growerId/plotId/specialistId/alertId does not exist.</response>
    [HttpPost]
    [ProducesResponseType(typeof(InterventionRequestResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateInterventionRequest(
        [FromBody] CreateInterventionRequestResource resource,
        [FromToken] int growerId,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateInterventionRequestCommand(
            growerId,
            resource.PlotId,
            resource.SpecialistId,
            resource.AlertId,
            resource.Reason,
            resource.Message);

        var result = await interventionRequestCommandService.Handle(command, cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Created(
                $"/api/v1/intervention-requests/{entity.Id}",
                InterventionRequestResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    /// <summary>
    ///     Gets an intervention request by id.
    /// </summary>
    /// <param name="id">The intervention request id.</param>
    /// <param name="growerId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Request found.</response>
    /// <response code="404">Request not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InterventionRequestResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterventionRequestById(
        [FromRoute] int id,
        [FromToken] int growerId,
        CancellationToken cancellationToken = default)
    {
        var result = await interventionRequestQueryService.Handle(new GetInterventionRequestByIdQuery(id, growerId), cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Ok(InterventionRequestResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    /// <summary>
    ///     Lists a grower's intervention requests, optionally narrowed to a
    ///     single plot (REQ-IREQ-2).
    /// </summary>
    /// <param name="growerId">The authenticated caller's id, derived from the token.</param>
    /// <param name="plotId">Optional plot id filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Requests returned (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InterventionRequestResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterventionRequests(
        [FromToken] int growerId,
        [FromQuery] long? plotId,
        CancellationToken cancellationToken = default)
    {
        var requests = await interventionRequestQueryService.Handle(
            new ListInterventionRequestsByGrowerQuery(growerId, plotId), cancellationToken);

        return Ok(requests.Select(InterventionRequestResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Declines an intervention request.
    /// </summary>
    /// <remarks>
    ///     The only supported target <c>status</c> is <c>DECLINED</c>. Per
    ///     REQ-IREQ-3 (OS parity), the transition is NOT self-guarded
    ///     against the current status — it succeeds regardless of the
    ///     request's current state.
    /// </remarks>
    /// <param name="id">The intervention request id.</param>
    /// <param name="growerId">The authenticated caller's id, derived from the token.</param>
    /// <param name="resource">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Request declined.</response>
    /// <response code="400">Missing/invalid <c>status</c> or <c>declineReason</c>.</response>
    /// <response code="404">Request not found.</response>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(InterventionRequestResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInterventionRequest(
        [FromRoute] int id,
        [FromToken] int growerId,
        [FromBody] DeclineInterventionRequestResource resource,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(resource.Status, "DECLINED", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(resource.DeclineReason))
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                InterventionErrors.ValidationError.Code,
                errorLocalizer[InterventionErrors.ValidationError.Code].Value ?? InterventionErrors.ValidationError.Message);
            return BadRequest(problemDetails);
        }

        var result = await interventionRequestCommandService.Handle(
            new DeclineInterventionRequestCommand(id, resource.DeclineReason, growerId), cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Ok(InterventionRequestResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    /// <summary>
    ///     The assigned specialist verifies (takes on) the request, moving
    ///     it out of their pending inbox while a proposal is prepared
    ///     (specialist-dashboard-parity).
    /// </summary>
    /// <param name="id">The intervention request id.</param>
    /// <param name="specialistId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Request verified (status <c>AWAITING_RESPONSE</c>).</response>
    /// <response code="404">Request not found, or not assigned to the caller.</response>
    [HttpPost("{id:int}/verifications")]
    [ProducesResponseType(typeof(InterventionRequestResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyInterventionRequest(
        [FromRoute] int id,
        [FromToken] int specialistId,
        CancellationToken cancellationToken = default)
    {
        var result = await interventionRequestCommandService.Handle(
            new VerifyInterventionRequestCommand(id, specialistId), cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Ok(InterventionRequestResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    /// <summary>
    ///     The assigned specialist declines the request. Distinct from the
    ///     grower-side decline (<c>PATCH /{id}</c>): here the actor is the
    ///     specialist, and a default reason is used when none is provided
    ///     (specialist-dashboard-parity).
    /// </summary>
    /// <param name="id">The intervention request id.</param>
    /// <param name="specialistId">The authenticated caller's id, derived from the token.</param>
    /// <param name="resource">Optional decline payload (<c>reason</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Request declined.</response>
    /// <response code="404">Request not found, or not assigned to the caller.</response>
    [HttpPost("{id:int}/declines")]
    [ProducesResponseType(typeof(InterventionRequestResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeclineInterventionRequestAsSpecialist(
        [FromRoute] int id,
        [FromToken] int specialistId,
        [FromBody] SpecialistDeclineResource? resource,
        CancellationToken cancellationToken = default)
    {
        var reason = string.IsNullOrWhiteSpace(resource?.Reason)
            ? "Declined by specialist"
            : resource.Reason;

        var result = await interventionRequestCommandService.Handle(
            new DeclineInterventionRequestAsSpecialistCommand(id, reason, specialistId), cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Ok(InterventionRequestResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }
}
