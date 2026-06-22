using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller that exposes IoT device endpoints scoped to a plot.
/// </summary>
/// <remarks>
///     (TS012TASK005) <c>GET /api/v1/plots/{plotId}/iot-devices</c> — lists all devices belonging to the specified plot.
///     (TS013TASK004) <c>POST /api/v1/plots/{plotId}/iot-devices</c> — registers a new IoT device under the specified plot.
///     (TS014) <c>PATCH /api/v1/plots/{plotId}/iot-devices/{deviceId}</c> — updates device name and status.
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
    ///     <c>201 Created</c> with the created <see cref="IoTDeviceResource"/>, 
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
                new { plotId = device.PlotId, userId = command.PlotId }, 
                IoTDeviceResourceFromEntityAssembler.ToResourceFromEntity(device)),
            error => error.Code == "PLOT_NOT_FOUND" 
                ? NotFound(error) 
                : BadRequest(error));
    }

    /// <summary>
    ///     Updates an existing IoT device's metadata under the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="deviceId">The device identifier (path variable).</param>
    /// <param name="resource">The request body with deviceName and status.</param>
    /// <returns>
    ///     <c>200 OK</c> with the updated <see cref="IoTDeviceResource"/>, 
    ///     or <c>400 Bad Request</c>/<c>404 Not Found</c> on failure.
    /// </returns>
    [HttpPatch("{deviceId:int}")]
    [Tags("IoTDevice")]
    [ProducesResponseType(typeof(IoTDeviceResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIoTDevice(
        [FromRoute] int plotId,
        [FromRoute] int deviceId,
        [FromBody] UpdateIoTDeviceResource resource)
    {
        var command = UpdateIoTDeviceCommandFromResourceAssembler.ToCommandFromResource(resource, plotId, deviceId);
        
        var result = await ioTDeviceCommandService.Handle(command);

        return result.Fold<IActionResult>(
            device => Ok(IoTDeviceResourceFromEntityAssembler.ToResourceFromEntity(device)),
            error => error.Code == "PLOT_NOT_FOUND" || error.Code == "DEVICE_NOT_FOUND"
                ? NotFound(error)
                : BadRequest(error));
    }

    /// <summary>
    ///     Deletes an existing IoT device from the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="deviceId">The device identifier (path variable).</param>
    /// <returns>
    ///     <c>204 NoContent</c> if deleted successfully, 
    ///     or <c>400 Bad Request</c>/<c>404 Not Found</c> on failure.
    /// </returns>
    [HttpDelete("{deviceId:int}")]
    [Tags("IoTDevice")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Error), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIoTDevice(
        [FromRoute] int plotId,
        [FromRoute] int deviceId)
    {
        // 1. Instanciar directamente el comando usando las variables numéricas de la ruta de la API
        var command = new DeleteIoTDeviceCommand(plotId, deviceId);

        // 2. Ejecutar el Handler correspondiente en la capa de aplicación
        var result = await ioTDeviceCommandService.Handle(command);

        // 3. Evaluar la respuesta para retornar códigos REST limpios
        return result.Fold<IActionResult>(
            success => NoContent(), // Las eliminaciones REST exitosas suelen responder con 204 No Content
            error => error.Code == "PLOT_NOT_FOUND" || error.Code == "DEVICE_NOT_FOUND"
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