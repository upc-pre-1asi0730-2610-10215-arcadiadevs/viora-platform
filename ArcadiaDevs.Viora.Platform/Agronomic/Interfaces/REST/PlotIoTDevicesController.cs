using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
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
///     (TS012TASK005) <c>GET /api/v1/plots/{plotId}/iot-devices</c> — lists all devices belonging to the specified plot.
///     (TS013TASK004) <c>POST /api/v1/plots/{plotId}/iot-devices</c> — registers a new IoT device under the specified plot.
/// </remarks>
[ApiController]
[Route("api/v1/plots/{plotId:int}/iot-devices")]
public class PlotIoTDevicesController(
    IIoTDeviceQueryService ioTDeviceQueryService,
    IIoTDeviceCommandService ioTDeviceCommandService) : ControllerBase
{
    /// <summary>
    ///     Registers a new IoT device under the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="resource">The request body with deviceName and optional status.</param>
    /// <returns>
    ///     <c>21 Created</c> with the created <see cref="IoTDeviceResource"/>, 
    ///     or <c>400 Bad Request</c>/<c>404 Not Found</c> on domain failure.
    /// </returns>
    [HttpPost]
    [Tags("IoTDevice")]
    [ProducesResponseType(typeof(IoTDeviceResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateIoTDevice(
        [FromRoute] int plotId,
        [FromBody] CreateIoTDeviceResource resource)
    {
        var command = CreateIoTDeviceCommandFromResourceAssembler.ToCommandFromResource(resource, plotId);
        var result = await ioTDeviceCommandService.Handle(command);

        return result.Fold<IActionResult>(
            device => CreatedAtAction(
                nameof(GetIoTDevicesByPlotId), 
                new { plotId = device.PlotId, userId = command.PlotId }, // O el parámetro que requiera tu GET si es aplicable
                IoTDeviceResourceFromEntityAssembler.ToResourceFromEntity(device)),
            error => error.Code == "PLOT_NOT_FOUND" // Check for PLOT_NOT_FOUND or similar codes
                ? NotFound(error) 
                : BadRequest(error));
    }

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