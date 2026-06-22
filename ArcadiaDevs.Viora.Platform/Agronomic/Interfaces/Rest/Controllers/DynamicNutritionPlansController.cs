using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for managing dynamic nutrition plans.
/// </summary>
[ApiController]
[Route("api/v1/dynamic-nutrition-plans")]
public class DynamicNutritionPlansController(
    IRecommendDynamicNutritionPlanCommandService recommendDynamicNutritionPlanCommandService,
    ICertifyNutritionApplicationCommandService certifyNutritionApplicationCommandService,
    IDynamicNutritionQueryService dynamicNutritionQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Recommends and generates a dynamic nutrition plan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecommendDynamicNutritionPlan(
        [FromQuery] int userId,
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

    /// <summary>
    ///     Certifies a dynamic nutrition plan application.
    /// </summary>
    [HttpPost("{planId}/certification")]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CertifyDynamicNutritionPlan(
        [FromQuery] int userId,
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

    /// <summary>
    ///     Gets the active dynamic nutrition plan for a plot.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveDynamicNutritionPlan(
        [FromQuery] int userId,
        [FromQuery] int plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetDynamicNutritionPlanQuery(plotId);
        var result = await dynamicNutritionQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plan => Ok(plan));
    }
}
