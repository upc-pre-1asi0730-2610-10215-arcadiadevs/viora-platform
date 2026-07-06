using System.Linq;
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
///     REST controller for treatment prescriptions (REQ-TP-1..5).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class TreatmentPrescriptionsController(
    ITreatmentPrescriptionCommandService treatmentPrescriptionCommandService,
    ITreatmentPrescriptionQueryService treatmentPrescriptionQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Creates a new treatment prescription for an accepted service proposal.
    /// </summary>
    /// <remarks>
    ///     Validates <c>serviceProposalId</c> exists (REQ-CC-2: missing FK
    ///     maps to 404). Per OS parity, creation is NOT blocked by the
    ///     proposal's status (REQ-TP-1). Fails 409 if a prescription already
    ///     exists for the given proposal (REQ-TP-4 idempotency).
    /// </remarks>
    /// <param name="resource">The creation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Prescription created (status <c>PENDING_INSPECTION</c>).</response>
    /// <response code="404">The referenced serviceProposalId does not exist.</response>
    /// <response code="409">A prescription already exists for the given serviceProposalId.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TreatmentPrescriptionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTreatmentPrescription(
        [FromBody] CreateTreatmentPrescriptionResource resource,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateTreatmentPrescriptionCommand(resource.ServiceProposalId);

        var result = await treatmentPrescriptionCommandService.Handle(command, cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            entity => Created(
                $"/api/v1/treatment-prescriptions/{entity.Id}",
                TreatmentPrescriptionResourceFromEntityAssembler.ToResourceFromEntity(entity)));
    }

    /// <summary>
    ///     Finds the (at most one) prescription linked to a service proposal
    ///     (REQ-TP-4 idempotency lookup, reused as a read endpoint).
    /// </summary>
    /// <param name="serviceProposalId">The service proposal id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Prescription found.</response>
    /// <response code="404">No prescription exists for the given serviceProposalId.</response>
    [HttpGet]
    [ProducesResponseType(typeof(TreatmentPrescriptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTreatmentPrescriptionByServiceProposalId(
        [FromQuery] int serviceProposalId,
        CancellationToken cancellationToken = default)
    {
        var prescription = await treatmentPrescriptionQueryService.Handle(
            new GetTreatmentPrescriptionByServiceProposalIdQuery(serviceProposalId), cancellationToken);

        if (prescription is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                InterventionErrors.NotFound.Code,
                InterventionErrors.NotFound.Message));
        }

        return Ok(TreatmentPrescriptionResourceFromEntityAssembler.ToResourceFromEntity(prescription));
    }

    /// <summary>
    ///     Gets a treatment prescription by id.
    /// </summary>
    /// <param name="id">The prescription id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Prescription found.</response>
    /// <response code="404">Prescription not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TreatmentPrescriptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTreatmentPrescriptionById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var prescription = await treatmentPrescriptionQueryService.Handle(
            new GetTreatmentPrescriptionByIdQuery(id), cancellationToken);

        if (prescription is null)
        {
            return NotFound(BuildProblemDetails(
                StatusCodes.Status404NotFound,
                InterventionErrors.NotFound.Code,
                InterventionErrors.NotFound.Message));
        }

        return Ok(TreatmentPrescriptionResourceFromEntityAssembler.ToResourceFromEntity(prescription));
    }

    /// <summary>
    ///     Logs the field inspection or issues the agrochemical prescription,
    ///     depending on <c>stage</c>.
    /// </summary>
    /// <remarks>
    ///     <c>stage=INSPECTION</c> logs the field inspection (REQ-TP-2,
    ///     self-guarded — only succeeds from <c>PENDING_INSPECTION</c>).
    ///     <c>stage=PRESCRIPTION</c> issues the agrochemical prescription
    ///     (REQ-TP-3, self-guarded — only succeeds from <c>INSPECTED</c>).
    ///     Mirrors <c>AlertsController.UpdateAlert</c>'s stage-branch PATCH
    ///     pattern (REQ-TP-5, design decision 2).
    /// </remarks>
    /// <param name="id">The prescription id.</param>
    /// <param name="resource">The update payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Stage transition applied.</response>
    /// <response code="400">Missing/invalid <c>stage</c>, or invalid field values.</response>
    /// <response code="404">Prescription not found.</response>
    /// <response code="409">The prescription is not in the expected status for the requested stage.</response>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(TreatmentPrescriptionResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTreatmentPrescription(
        [FromRoute] int id,
        [FromBody] UpdateTreatmentPrescriptionResource resource,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(resource.Stage, "INSPECTION", StringComparison.OrdinalIgnoreCase))
        {
            var command = new LogFieldInspectionCommand(
                id,
                resource.FindingType,
                resource.IncidenceLevel,
                resource.TechnicalDescription,
                resource.RecordDate);

            var result = await treatmentPrescriptionCommandService.Handle(command, cancellationToken);

            return InterventionActionResultAssembler.ToActionResult(
                this,
                result,
                errorLocalizer,
                problemDetailsFactory,
                entity => Ok(TreatmentPrescriptionResourceFromEntityAssembler.ToResourceFromEntity(entity)));
        }

        if (string.Equals(resource.Stage, "PRESCRIPTION", StringComparison.OrdinalIgnoreCase))
        {
            var command = new PrescribeAgrochemicalCommand(
                id,
                resource.ApplicationMethod,
                resource.SprayVolumeAmount,
                resource.SprayVolumeUnit,
                resource.PreHarvestIntervalDays,
                resource.AgronomistRecommendations,
                resource.RequiredPPE,
                resource.Products?.Select(p => new PrescribedProductCommandItem(
                    p.ProductName,
                    p.DosageAmount,
                    p.DosageUnit,
                    p.SessionsCount,
                    p.TechnicalRecommendation)).ToList());

            var result = await treatmentPrescriptionCommandService.Handle(command, cancellationToken);

            return InterventionActionResultAssembler.ToActionResult(
                this,
                result,
                errorLocalizer,
                problemDetailsFactory,
                entity => Ok(TreatmentPrescriptionResourceFromEntityAssembler.ToResourceFromEntity(entity)));
        }

        var problemDetails = BuildProblemDetails(
            StatusCodes.Status400BadRequest,
            InterventionErrors.ValidationError.Code,
            InterventionErrors.ValidationError.Message);
        return BadRequest(problemDetails);
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
