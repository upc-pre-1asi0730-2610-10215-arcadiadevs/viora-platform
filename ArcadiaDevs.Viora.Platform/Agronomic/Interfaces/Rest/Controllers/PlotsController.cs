using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for plot operations.
///     View representation is dispatched via <c>?view=</c> query parameter.
/// </summary>
[ApiController]
[Route("api/v1/plots")]
[Produces("application/json")]
[Authorize]
public class PlotsController(
    IPlotCommandService plotCommandService,
    IPlotQueryService plotQueryService,
    IGetPlotDetailQueryService getPlotDetailQueryService,
    IGetPlotMonitoringSummaryQueryService getPlotMonitoringSummaryQueryService,
    IGetPlotWeatherForecastQueryService getPlotWeatherForecastQueryService,
    IGetPlotNdviTileQueryService getPlotNdviTileQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Creates a new plot with geospatial polygon coordinates.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatedPlotResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plot => Created($"/api/v1/plots/{plot.Id}", plot.ToCreatedResource()));
    }

    /// <summary>
    ///     Declares the winter-chill requirement for a plot.
    /// </summary>
    [HttpPut("{plotId:int}/chill-requirement")]
    [ProducesResponseType(typeof(ChillRequirementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigureChillRequirement(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        [FromBody] ConfigureChillRequirementResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand(plotId, userId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            chillRequirement => Ok(chillRequirement.ToResource()));
    }

    /// <summary>
    ///     Clears a plot's declared chill requirement.
    /// </summary>
    [HttpDelete("{plotId:int}/chill-requirement")]
    [ProducesResponseType(typeof(ChillRequirementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetChillRequirement(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var command = new ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands.ResetChillRequirementCommand(plotId, userId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            chillRequirement => Ok(chillRequirement.ToResource()));
    }

    /// <summary>
    ///     Updates an existing plot.
    /// </summary>
    [HttpPatch("{plotId:int}")]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlot(
        [FromRoute] int plotId,
        [FromBody] UpdatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommandFromResource(plotId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plot => Ok(plot.ToResource()));
    }

    /// <summary>
    ///     Deletes an existing plot.
    /// </summary>
    [HttpDelete("{plotId:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlot(
        [FromRoute] int plotId,
        CancellationToken cancellationToken)
    {
        var command = new ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands.DeletePlotCommand(plotId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            message => Ok(message));
    }

    /// <summary>
    ///     Gets satellite imagery tile for a plot.
    /// </summary>
    [HttpGet("{plotId:int}/imagery/tile/{zoom:int}/{x:int}/{y:int}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "image/png")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImageryTile(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        [FromRoute] int zoom,
        [FromRoute] int x,
        [FromRoute] int y,
        CancellationToken cancellationToken)
    {
        var query = new GetPlotNdviTileQuery(userId, plotId, zoom, x, y);
        var result = await getPlotNdviTileQueryService.HandleAsync(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            bytes => File(bytes, "image/png"));
    }

    /// <summary>
    ///     Gets plots owned by a user, or a summary overview when <c>?view=overview</c>.
    ///     When <c>?includeCurrentImagery=true</c>, returns <see cref="PlotWithCurrentImageryResource"/>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlotResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<PlotWithCurrentImageryResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MyPlotsOverviewResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserPlots(
        [FromQuery] int userId,
        [FromQuery] PlotView? view,
        [FromQuery] bool includeCurrentImagery = false,
        CancellationToken cancellationToken = default)
    {
        if (view == PlotView.Overview)
        {
            var overviewQuery = new GetMyPlotsOverviewQuery(userId);
            var overviewResult = await plotQueryService.Handle(overviewQuery, cancellationToken);

            return AgronomicActionResultAssembler.ToActionResult(
                this,
                overviewResult,
                errorLocalizer,
                problemDetailsFactory,
                overview => Ok(overview));
        }

        if (view is not null)
            return BadRequest(new { error = $"Invalid view '{view}' for list endpoint. Valid values: overview." });

        if (includeCurrentImagery)
        {
            var imageryQuery = new GetPlotsWithCurrentImageryQuery(userId);
            var imageryResult = await plotQueryService.Handle(imageryQuery, cancellationToken);

            return AgronomicActionResultAssembler.ToActionResult(
                this,
                imageryResult,
                errorLocalizer,
                problemDetailsFactory,
                plots => Ok(plots));
        }

        var query = new GetPlotsByUserIdQuery(userId);
        var result = await plotQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plots => Ok(plots));
    }

    /// <summary>
    ///     Gets a specific plot by its ID, or a view-specific representation
    ///     when <c>?view=detail</c>, <c>monitoring</c>, or <c>weather</c>.
    /// </summary>
    [HttpGet("{plotId:int}")]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PlotDetailResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PlotMonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PlotWeatherForecastResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlotById(
        [FromRoute] int plotId,
        [FromQuery] PlotView? view,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        switch (view)
        {
            case PlotView.Detail:
            {
                var query = new GetPlotDetailQuery(plotId, userId);
                var result = await getPlotDetailQueryService.Handle(query, cancellationToken);

                return AgronomicActionResultAssembler.ToActionResult(
                    this,
                    result,
                    errorLocalizer,
                    problemDetailsFactory,
                    detail => Ok(detail));
            }
            case PlotView.Monitoring:
            {
                var query = new GetPlotMonitoringSummaryQuery(plotId, userId);
                var result = await getPlotMonitoringSummaryQueryService.Handle(query, cancellationToken);

                return AgronomicActionResultAssembler.ToActionResult(
                    this,
                    result,
                    errorLocalizer,
                    problemDetailsFactory,
                    summary => Ok(summary));
            }
            case PlotView.Weather:
            {
                var query = new GetPlotWeatherForecastQuery(plotId, userId);
                var result = await getPlotWeatherForecastQueryService.Handle(query, cancellationToken);

                return AgronomicActionResultAssembler.ToActionResult(
                    this,
                    result,
                    errorLocalizer,
                    problemDetailsFactory,
                    forecast => Ok(forecast));
            }
            case null:
            {
                var query = new GetPlotByIdQuery(plotId);
                var result = await plotQueryService.Handle(query, cancellationToken);

                return AgronomicActionResultAssembler.ToActionResult(
                    this,
                    result,
                    errorLocalizer,
                    problemDetailsFactory,
                    plot => Ok(plot));
            }
            default:
                return BadRequest(new { error = $"Invalid view '{view}'. Valid values: detail, monitoring, weather." });
        }
    }
}
