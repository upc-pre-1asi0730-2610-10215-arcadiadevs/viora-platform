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
