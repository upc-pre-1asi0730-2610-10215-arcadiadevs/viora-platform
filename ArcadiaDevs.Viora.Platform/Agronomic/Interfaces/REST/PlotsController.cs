using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.DTOs;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

using Microsoft.AspNetCore.Mvc;

/// <summary>
///     REST controller for plot operations.
/// </summary>
[ApiController]
[Route("api/v1/plots")]
[Produces("application/json")]
public class PlotsController(
    IPlotCommandService plotCommandService,
    IDynamicNutritionQueryService dynamicNutritionQueryService) : ControllerBase
{
    /// <summary>
    ///     Creates a new plot with geospatial polygon coordinates.
    /// </summary>
    /// <param name="resource">The create plot resource.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="201">Plot created successfully</response>
    /// <response code="400">Invalid request data</response>
    [HttpPost]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            plot => Created($"/api/v1/plots/{plot.Id}", plot.ToResource()),
            error => BadRequest(new { }));
    }

    /// <summary>
    ///     Returns the active nutrition plan for a specific plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Active nutrition plan retrieved</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Plot not found</response>
    [HttpGet("{plotId:int}/dynamic-nutrition/active-plan")]
    [ProducesResponseType(typeof(DynamicNutritionPlanDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDynamicNutritionPlan(
        [FromRoute] int plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetDynamicNutritionPlanQuery(plotId);
        var result = await dynamicNutritionQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            plan => Ok(plan),
            error => error.Code == "PLOT_NOT_FOUND"
                ? NotFound(new { })
                : BadRequest(new { }));
    }

    /// <summary>
    ///     Declares the winter-chill requirement for a plot.
    /// </summary>
    /// <response code="200">Chill requirement configured</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">User does not own the plot</response>
    /// <response code="404">Plot not found</response>
    [HttpPut("{plotId:int}/chill-requirement")]
    [ProducesResponseType(typeof(ChillRequirementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigureChillRequirement(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        [FromBody] ConfigureChillRequirementResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand(plotId, userId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            chillRequirement => Ok(chillRequirement.ToResource()),
            error => error.Code switch
            {
                "PLOT_NOT_FOUND" => NotFound(new { }),
                "UNAUTHORIZED_ACCESS" => StatusCode(403, new { }),
                _ => BadRequest(new { })
            });
    }

    /// <summary>
    ///     Clears a plot's declared chill requirement.
    /// </summary>
    /// <response code="200">Chill requirement reset successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="403">User does not own the plot</response>
    /// <response code="404">Plot not found</response>
    [HttpDelete("{plotId:int}/chill-requirement")]
    [ProducesResponseType(typeof(ChillRequirementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetChillRequirement(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var command = new ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands.ResetChillRequirementCommand(plotId, userId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            chillRequirement => Ok(chillRequirement.ToResource()),
            error => error.Code switch
            {
                "PLOT_NOT_FOUND" => NotFound(new { }),
                "UNAUTHORIZED_ACCESS" => StatusCode(403, new { }),
                _ => BadRequest(new { })
            });
    }
}
