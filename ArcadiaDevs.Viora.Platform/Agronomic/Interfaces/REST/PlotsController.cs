using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
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
    IGetMyPlotsOverviewQueryService getMyPlotsOverviewQueryService,
    IGetPlotDetailQueryService getPlotDetailQueryService,
    IGetPlotMonitoringSummaryQueryService getPlotMonitoringSummaryQueryService,
    IGetPlotWeatherForecastQueryService getPlotWeatherForecastQueryService,
    IGetPlotsByUserIdQueryService getPlotsByUserIdQueryService,
    IGetPlotByIdQueryService getPlotByIdQueryService,
    IGetPlotNdviTileQueryService getPlotNdviTileQueryService) : ControllerBase
{
    /// <summary>
    ///     Creates a new plot with geospatial polygon coordinates.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatedPlotResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            plot => Created($"/api/v1/plots/{plot.Id}", plot.ToCreatedResource()),
            error => BadRequest(new { }));
    }

    /// <summary>
    ///     Declares the winter-chill requirement for a plot.
    /// </summary>
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

    /// <summary>
    ///     Updates an existing plot.
    /// </summary>
    [HttpPatch("{plotId:int}")]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlot(
        [FromRoute] int plotId,
        [FromBody] UpdatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommandFromResource(plotId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            plot => Ok(plot.ToResource()),
            error => error.Code switch
            {
                "PLOT_NOT_FOUND" => NotFound(new { }),
                _ => BadRequest(new { })
            });
    }

    /// <summary>
    ///     Deletes an existing plot.
    /// </summary>
    [HttpDelete("{plotId:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlot(
        [FromRoute] int plotId,
        CancellationToken cancellationToken)
    {
        var command = new ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands.DeletePlotCommand(plotId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            message => Ok(message),
            error => error.Code switch
            {
                "PLOT_NOT_FOUND" => NotFound(new { }),
                _ => BadRequest(new { })
            });
    }

    /// <summary>
    ///     Gets a summary overview of a user's plots.
    /// </summary>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(MyPlotsOverviewResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOverview(
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetMyPlotsOverviewQuery(userId);
        var result = await getMyPlotsOverviewQueryService.Handle(query, cancellationToken);
        return result.Fold<IActionResult>(Ok, _ => BadRequest(new { }));
    }

    /// <summary>
    ///     Gets detailed information of a specific plot.
    /// </summary>
    [HttpGet("{plotId:int}/detail")]
    [ProducesResponseType(typeof(PlotDetailResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetail(
        [FromRoute] int plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlotDetailQuery(plotId);
        var result = await getPlotDetailQueryService.Handle(query, cancellationToken);
        return result.Fold<IActionResult>(Ok, _ => NotFound(new { }));
    }

    /// <summary>
    ///     Gets the monitoring summary of a specific plot.
    /// </summary>
    [HttpGet("{plotId:int}/monitoring-summary")]
    [ProducesResponseType(typeof(PlotMonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMonitoringSummary(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlotMonitoringSummaryQuery(plotId, userId);
        var result = await getPlotMonitoringSummaryQueryService.Handle(query, cancellationToken);
        return result.Fold<IActionResult>(Ok, _ => NotFound(new { }));
    }

    /// <summary>
    ///     Gets the weather forecast for a specific plot.
    /// </summary>
    [HttpGet("{plotId:int}/weather-forecast")]
    [ProducesResponseType(typeof(PlotWeatherForecastResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWeatherForecast(
        [FromRoute] int plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlotWeatherForecastQuery(plotId);
        var result = await getPlotWeatherForecastQueryService.Handle(query, cancellationToken);
        return result.Fold<IActionResult>(Ok, _ => NotFound(new { }));
    }

    /// <summary>
    ///     Gets satellite imagery tile for a plot.
    /// </summary>
    [HttpGet("{plotId:int}/imagery/tile/{zoom:int}/{x:int}/{y:int}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "image/png")]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
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
        
        return result.Fold<IActionResult>(
            bytes => File(bytes, "image/png"),
            _ => NotFound(new { })
        );
    }

    /// <summary>
    ///     Gets all plots owned by a specific user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlotResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPlots(
        [FromQuery] int userId,
        [FromQuery] bool includeCurrentImagery = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPlotsByUserIdQuery(userId, includeCurrentImagery);
        var result = await getPlotsByUserIdQueryService.Handle(query, cancellationToken);
        return result.Fold<IActionResult>(Ok, _ => BadRequest(new { }));
    }

    /// <summary>
    ///     Gets a specific plot by its ID.
    /// </summary>
    [HttpGet("{plotId:int}")]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlotById(
        [FromRoute] int plotId,
        CancellationToken cancellationToken)
    {
        var query = new GetPlotByIdQuery(plotId);
        var result = await getPlotByIdQueryService.Handle(query, cancellationToken);
        return result.Fold<IActionResult>(Ok, _ => NotFound(new { }));
    }
}
