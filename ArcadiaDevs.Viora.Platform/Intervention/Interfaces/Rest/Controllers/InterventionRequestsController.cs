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
public class InterventionRequestsController(
    IInterventionRequestCommandService interventionRequestCommandService,
    IInterventionRequestQueryService interventionRequestQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
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
