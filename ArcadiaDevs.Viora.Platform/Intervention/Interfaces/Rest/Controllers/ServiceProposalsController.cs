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
///     REST controller for service proposals (REQ-SP-1..4).
/// </summary>
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
    /// <summary>
    ///     Submits a new service proposal for an intervention request.
    /// </summary>
    /// <remarks>
    ///     Validates <c>interventionRequestId</c>/<c>specialistId</c>
    ///     through their repositories (REQ-CC-2: missing FK maps to 404)
    ///     and the <c>costEstimate</c> fields (REQ-CC-2: validation failure
    ///     maps to 400). Side-effects the parent intervention request to
    ///     <c>PROPOSAL_RECEIVED</c> (REQ-SP-1).
    /// </remarks>
    /// <param name="resource">The proposal payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Proposal created (status <c>PENDING</c>).</response>
    /// <response code="400">Validation failure (e.g. negative cost estimate).</response>
    /// <response code="404">The referenced interventionRequestId/specialistId does not exist.</response>
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

    /// <summary>
    ///     Lists service proposals for an intervention request (REQ-SP-4).
    /// </summary>
    /// <param name="requestId">The intervention request id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Proposals returned (possibly empty).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceProposalResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetServiceProposals(
        [FromQuery] int requestId,
        CancellationToken cancellationToken = default)
    {
        var proposals = await serviceProposalQueryService.Handle(
            new ListServiceProposalsByRequestQuery(requestId), cancellationToken);

        return Ok(proposals.Select(ServiceProposalResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>
    ///     Accepts or rejects a service proposal.
    /// </summary>
    /// <remarks>
    ///     The only supported target <c>status</c> values are
    ///     <c>ACCEPTED</c> (REQ-SP-2) and <c>REJECTED</c> (REQ-SP-3); both
    ///     are self-guarded on the aggregate (PENDING only, 409 otherwise)
    ///     and side-effect the parent intervention request (
    ///     <c>ACCEPTED</c> → request <c>ACCEPTED</c>; <c>REJECTED</c> →
    ///     request terminal <c>DECLINED</c>, no re-routing).
    /// </remarks>
    /// <param name="id">The service proposal id.</param>
    /// <param name="resource">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Proposal accepted/rejected.</response>
    /// <response code="400">Missing/invalid <c>status</c>.</response>
    /// <response code="404">Proposal not found.</response>
    /// <response code="409">The proposal is not in <c>PENDING</c>.</response>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(ServiceProposalResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateServiceProposal(
        [FromRoute] int id,
        [FromBody] UpdateServiceProposalResource resource,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(resource.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
        {
            var result = await serviceProposalCommandService.Handle(
                new AcceptServiceProposalCommand(id), cancellationToken);

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
                new RejectServiceProposalCommand(id), cancellationToken);

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
