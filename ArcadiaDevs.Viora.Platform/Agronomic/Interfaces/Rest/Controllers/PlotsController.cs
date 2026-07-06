using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
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
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory,
    IClock clock) : ControllerBase
{
    /// <summary>
    ///     Creates a new plot with geospatial polygon coordinates.
    /// </summary>
    /// <param name="resource">The request body with the plot data and polygon coordinates.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="201">Plot created.</response>
    /// <response code="400">Validation failure.</response>
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
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="resource">The request body with the chill requirement data.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Chill requirement configured.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="403">The user does not own the plot.</response>
    /// <response code="404">Plot not found.</response>
    [HttpPut("{plotId:int}/chill-requirement")]
    [ProducesResponseType(typeof(ChillRequirementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfigureChillRequirement(
        [FromRoute] int plotId,
        [FromToken] int userId,
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
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Chill requirement cleared.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="403">The user does not own the plot.</response>
    /// <response code="404">Plot not found.</response>
    [HttpDelete("{plotId:int}/chill-requirement")]
    [ProducesResponseType(typeof(ChillRequirementResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetChillRequirement(
        [FromRoute] int plotId,
        [FromToken] int userId,
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
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="resource">The request body with the fields to update.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Plot updated.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="403">The user does not own the plot.</response>
    /// <response code="404">Plot not found.</response>
    [HttpPatch("{plotId:int}")]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlot(
        [FromRoute] int plotId,
        [FromToken] int userId,
        [FromBody] UpdatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommandFromResource(plotId, userId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            plot => Ok(plot.ToResource(clock)));
    }

    /// <summary>
    ///     Deletes an existing plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Plot deleted; returns a confirmation message.</response>
    /// <response code="400">Validation failure.</response>
    /// <response code="403">The user does not own the plot.</response>
    /// <response code="404">Plot not found.</response>
    [HttpDelete("{plotId:int}")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlot(
        [FromRoute] int plotId,
        [FromToken] int userId,
        CancellationToken cancellationToken)
    {
        var command = new ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands.DeletePlotCommand(plotId, userId);
        var result = await plotCommandService.Handle(command, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            message => Ok(message));
    }

    /// <summary>
    ///     Gets plots owned by a user, or a summary overview when <c>?view=overview</c>.
    ///     When <c>?includeCurrentImagery=true</c>, returns <see cref="PlotWithCurrentImageryResource"/>.
    /// </summary>
    /// <remarks>
    ///     Response shape depends on <c>view</c> and <c>includeCurrentImagery</c>:
    ///     <list type="bullet">
    ///         <item><description><c>view</c> omitted and <c>includeCurrentImagery=false</c> (default) — a plain list of <see cref="PlotResource"/> for the user.</description></item>
    ///         <item><description><c>includeCurrentImagery=true</c> — a list of <see cref="PlotWithCurrentImageryResource"/>, each plot enriched with its current imagery.</description></item>
    ///         <item><description><c>view=overview</c> — a single <see cref="MyPlotsOverviewResource"/> summarizing all of the user's plots.</description></item>
    ///     </list>
    ///     Any other <c>view</c> value returns <c>400 Bad Request</c>.
    /// </remarks>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="view">Optional view mode; only <c>overview</c> is supported for this endpoint (query parameter).</param>
    /// <param name="includeCurrentImagery">When <c>true</c>, enriches each plot with its current imagery (query parameter).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Plots (or overview) returned. See remarks for the response shape per <c>view</c>/<c>includeCurrentImagery</c>.</response>
    /// <response code="400">Invalid <c>view</c> value for this endpoint.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PlotResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<PlotWithCurrentImageryResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MyPlotsOverviewResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserPlots(
        [FromToken] int userId,
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
    /// <remarks>
    ///     Response shape depends on <c>view</c>:
    ///     <list type="bullet">
    ///         <item><description><c>view</c> omitted — a plain <see cref="PlotResource"/> for the plot.</description></item>
    ///         <item><description><c>view=detail</c> — a <see cref="PlotDetailResource"/> with the plot's extended detail data.</description></item>
    ///         <item><description><c>view=monitoring</c> — a <see cref="PlotMonitoringSummaryResource"/> with the plot's monitoring summary.</description></item>
    ///         <item><description><c>view=weather</c> — a <see cref="PlotWeatherForecastResource"/> with the plot's weather forecast.</description></item>
    ///     </list>
    ///     Any other <c>view</c> value returns <c>400 Bad Request</c>.
    /// </remarks>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="view">Optional view mode: <c>detail</c>, <c>monitoring</c>, or <c>weather</c> (query parameter).</param>
    /// <param name="userId">The user identifier (query parameter).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <response code="200">Plot (or view-specific representation) returned. See remarks for the response shape per <c>view</c>.</response>
    /// <response code="400">Invalid <c>view</c> value.</response>
    /// <response code="403">The user does not have access to the requested plot.</response>
    /// <response code="404">Plot not found.</response>
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
        [FromToken] int userId,
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
                var query = new GetPlotByIdQuery(plotId, userId);
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
