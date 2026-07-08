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
public class ServiceProposalsController(
    IServiceProposalCommandService serviceProposalCommandService,
    IServiceProposalQueryService serviceProposalQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ServiceProposalResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitServiceProposal(
        [FromBody] SubmitServiceProposalResource resource,
        CancellationToken cancellationToken = default)
    {
        var command = new SubmitServiceProposalCommand(
            resource.InterventionRequestId,
            resource.SpecialistId,
            resource.ServiceTitle,
            resource.DurationLabel,
            resource.Scope,
            resource.ProposedDate,
            resource.CostAmount,
            resource.CostCurrency,
            resource.ProposalDetails);

        var result = await serviceProposalCommandService.Handle(command, cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Created(
                $"/api/v1/service-proposals/{entity.Id}",
                ServiceProposalResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceProposalResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceProposals(
        [FromQuery] int requestId,
        [FromToken] int growerId,
        CancellationToken cancellationToken = default)
    {
        var proposals = await serviceProposalQueryService.Handle(
            new ListServiceProposalsByRequestQuery(requestId, growerId), cancellationToken);

        return Ok(proposals.Select(ServiceProposalResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ServiceProposalResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateServiceProposal(
        [FromRoute] int id,
        [FromToken] int growerId,
        [FromBody] UpdateServiceProposalResource resource,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(resource.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
        {
            var result = await serviceProposalCommandService.Handle(
                new AcceptServiceProposalCommand(id, growerId), cancellationToken);

            return InterventionActionResultAssembler.ToActionResult(
                this,
                result,
                errorLocalizer,
                problemDetailsFactory,
                entity => Ok(ServiceProposalResourceFromEntityAssembler.ToResourceFromEntity(entity)));
        }

        if (string.Equals(resource.Status, "REJECTED", StringComparison.OrdinalIgnoreCase))
        {
            var result = await serviceProposalCommandService.Handle(
                new RejectServiceProposalCommand(id, growerId), cancellationToken);

            return InterventionActionResultAssembler.ToActionResult(
                this,
                result,
                errorLocalizer,
                problemDetailsFactory,
                entity => Ok(ServiceProposalResourceFromEntityAssembler.ToResourceFromEntity(entity)));
        }

        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            HttpContext,
            StatusCodes.Status400BadRequest,
            InterventionErrors.ValidationError.Code,
            errorLocalizer[InterventionErrors.ValidationError.Code].Value ?? InterventionErrors.ValidationError.Message);
        return BadRequest(problemDetails);
    }
}
