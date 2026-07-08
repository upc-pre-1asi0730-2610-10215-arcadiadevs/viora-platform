using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/dynamic-nutrition-plans")]
[Authorize]
public class DynamicNutritionPlansController(
    IRecommendDynamicNutritionPlanCommandService recommendDynamicNutritionPlanCommandService,
    ICertifyNutritionApplicationCommandService certifyNutritionApplicationCommandService,
    IDynamicNutritionQueryService dynamicNutritionQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecommendDynamicNutritionPlan(
        [FromToken] int userId,
        [FromQuery] int plotId,
        CancellationToken cancellationToken)
    {
        var command = new RecommendDynamicNutritionCommand(userId, plotId);
        var result = await recommendDynamicNutritionPlanCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plan => StatusCode(StatusCodes.Status201Created, DynamicNutritionPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    [HttpPatch("{planId}")]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CertifyDynamicNutritionPlan(
        [FromToken] int userId,
        long planId,
        [FromBody] CertifyNutritionApplicationResource resource,
        CancellationToken cancellationToken)
    {
        var command = new CertifyNutritionApplicationCommand(
            userId,
            planId,
            resource.ApplicationDate,
            resource.ApplicationTime,
            resource.AppliedInputs,
            resource.DoseConfirmation,
            resource.FieldOperator,
            resource.FieldNotes);
        var result = await certifyNutritionApplicationCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plan => Ok(DynamicNutritionPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)));
    }

    [HttpGet]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveDynamicNutritionPlan(
        [FromToken] int userId,
        [FromQuery] int plotId,
        [FromQuery] string status = "ACTIVE",
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = $"Unsupported status '{status}'. Only 'ACTIVE' is supported." });
        }

        var query = new GetDynamicNutritionPlanQuery(plotId, userId);
        var result = await dynamicNutritionQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plan => Ok(plan));
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetActiveDynamicNutritionPlanLegacy(
        [FromToken] int userId,
        [FromQuery] int plotId,
        CancellationToken cancellationToken = default) =>
        GetActiveDynamicNutritionPlan(userId, plotId, "ACTIVE", cancellationToken);
}
