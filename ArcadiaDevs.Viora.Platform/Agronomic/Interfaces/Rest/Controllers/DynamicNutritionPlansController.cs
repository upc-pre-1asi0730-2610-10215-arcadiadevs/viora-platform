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

/// <summary>
///     REST controller for managing dynamic nutrition plans.
/// </summary>
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
    /// <summary>
    ///     Recommends and generates a dynamic nutrition plan.
    /// </summary>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="plotId">The plot identifier (query parameter).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="201">Nutrition plan recommended and created.</response>
    /// <response code="400">Recommendation failed due to invalid input or domain failure.</response>
    /// <response code="403">The user does not own the plot.</response>
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

    /// <summary>
    ///     Certifies a dynamic nutrition plan application.
    /// </summary>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="planId">The dynamic nutrition plan identifier (path variable).</param>
    /// <param name="resource">The certification payload.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Application certified.</response>
    /// <response code="400">Missing or invalid certification data.</response>
    /// <response code="403">The user does not own the plan's plot.</response>
    /// <response code="422">The plan cannot be certified in its current state.</response>
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

    /// <summary>
    ///     Gets the active dynamic nutrition plan for a plot (REQ parity with OS:
    ///     root GET + <c>?status=ACTIVE</c>, not a dedicated sub-route). WA only
    ///     supports the <c>ACTIVE</c> filter today — any other explicit value is
    ///     rejected rather than silently returning an empty stub list like OS
    ///     does, since WA has no "list all plans" feature behind this endpoint yet.
    /// </summary>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="plotId">The plot identifier (query parameter).</param>
    /// <param name="status">Optional status filter; defaults to <c>ACTIVE</c>, the only supported value.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Active nutrition plan found.</response>
    /// <response code="400">Invalid request parameters, or an unsupported status filter.</response>
    /// <response code="403">The user does not own the plot.</response>
    /// <response code="404">No active nutrition plan found for the plot.</response>
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

    /// <summary>
    ///     Legacy alias for <see cref="GetActiveDynamicNutritionPlan"/>. Kept so
    ///     existing clients hitting the old dedicated sub-route keep working
    ///     without a coordinated frontend change; new clients should use the
    ///     root route with <c>?status=ACTIVE</c>.
    /// </summary>
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
