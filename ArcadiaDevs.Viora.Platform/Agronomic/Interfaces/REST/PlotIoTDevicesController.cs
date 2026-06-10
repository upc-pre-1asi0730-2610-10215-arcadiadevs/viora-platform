using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest;

/// <summary>
///     REST controller that exposes IoT device endpoints scoped to a plot.
/// </summary>
/// <remarks>
///     (TS012TASK005) <c>GET /api/v1/plots/{plotId}/iot-devices</c> — lists all devices
///     belonging to the specified plot. Requires the authenticated user to own the plot.
/// </remarks>
[ApiController]
[Route("api/v1/plots/{plotId:int}/iot-devices")]
public class PlotIoTDevicesController(IIoTDeviceQueryService ioTDeviceQueryService) : ControllerBase
{
    /// <summary>
    ///     Returns all IoT devices registered under the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The authenticated user identifier (query parameter).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>
    ///     <c>200 OK</c> with a list of <see cref="IoTDeviceResource"/>, or
    ///     <c>403 Forbidden</c> when the user does not own the plot.
    /// </returns>
    [HttpGet]
    [Tags("IoTDevice")]
    [ProducesResponseType(typeof(IEnumerable<IoTDeviceResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetIoTDevicesByPlotId(
        [FromRoute] int plotId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetIoTDevicesByPlotIdQuery(plotId, userId);
        var result = await ioTDeviceQueryService.Handle(query, cancellationToken);

        return result.Fold<IActionResult>(
            devices => Ok(devices.Select(d => d.ToResourceFromEntity())),
            error => StatusCode(StatusCodes.Status403Forbidden, error));
    }
}
