using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest;

/// <summary>
///     REST controller for plot operations.
/// </summary>
[ApiController]
[Route("api/v1/plots")]
public class PlotsController(IPlotCommandService plotCommandService) : ControllerBase
{
    /// <summary>
    ///     Creates a new plot with geospatial polygon coordinates.
    /// </summary>
    /// <param name="resource">The create plot resource.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>201 Created with the plot resource, or 400 Bad Request with error details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PlotResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePlotResource resource,
        CancellationToken cancellationToken)
    {
        var command = resource.ToCommand();
        var result = await plotCommandService.Handle(command, cancellationToken);

        return result.Fold<IActionResult>(
            plot => Created($"/api/v1/plots/{plot.Id}", plot.ToResource()),
            error => BadRequest(error));
    }
}
