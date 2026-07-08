using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/plots/{plotId:int}/images")]
[Produces("application/json")]
[Authorize]
public class PlotImageryTilesController(
    IGetPlotNdviTileQueryService getPlotNdviTileQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [Produces("image/png", "application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "image/png")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentNdviTile(
        [FromRoute] int plotId,
        [FromQuery] int zoom,
        [FromQuery] int x,
        [FromQuery] int y,
        [FromToken] int userId,
        CancellationToken cancellationToken = default)
    {
        Response.Headers["Cache-Control"] = "private, max-age=1800";

        var query = new GetPlotNdviTileQuery(userId, plotId, zoom, x, y);
        var result = await getPlotNdviTileQueryService.HandleAsync(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            bytes => File(bytes, "image/png"));
    }
}
