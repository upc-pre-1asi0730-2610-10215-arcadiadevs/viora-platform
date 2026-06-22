using System.Threading;
using System.Threading.Tasks;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for managing dynamic nutrition plans.
/// </summary>
[ApiController]
[Route("api/v1/dynamic-nutrition-plans")]
public class DynamicNutritionPlansController(
    IRecommendDynamicNutritionPlanCommandService recommendDynamicNutritionPlanCommandService,
    ICertifyNutritionApplicationCommandService certifyNutritionApplicationCommandService,
    IDynamicNutritionQueryService dynamicNutritionQueryService) : ControllerBase
{
    /// <summary>
    ///     Recommends and generates a dynamic nutrition plan.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecommendDynamicNutritionPlan(
        [FromQuery] int userId,
        [FromQuery] int plotId,
        CancellationToken cancellationToken)
    {
        var command = new RecommendDynamicNutritionCommand(userId, plotId);
        var result = await recommendDynamicNutritionPlanCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            plan => StatusCode(StatusCodes.Status201Created, DynamicNutritionPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)),
            error => BadRequest(error));
    }

    /// <summary>
    ///     Certifies a dynamic nutrition plan application.
    /// </summary>
    [HttpPost("{planId}/certification")]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
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

        return result.Fold<IActionResult>(
            plan => Ok(DynamicNutritionPlanResourceFromEntityAssembler.ToResourceFromEntity(plan)),
            error => BadRequest(error));
    }

    /// <summary>
    ///     Gets the active dynamic nutrition plan for a plot.
    /// </summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(DynamicNutritionPlanResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveDynamicNutritionPlan(
        [FromQuery] int userId,
        [FromQuery] int plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetDynamicNutritionPlanQuery(plotId);
        var result = await dynamicNutritionQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            plan => Ok(plan),
            error => error.Code == "PLAN_NOT_FOUND" ? NotFound(error) : BadRequest(error));
    }
}
