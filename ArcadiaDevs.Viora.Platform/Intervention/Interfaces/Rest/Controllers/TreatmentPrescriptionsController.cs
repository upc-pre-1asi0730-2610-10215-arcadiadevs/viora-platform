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
