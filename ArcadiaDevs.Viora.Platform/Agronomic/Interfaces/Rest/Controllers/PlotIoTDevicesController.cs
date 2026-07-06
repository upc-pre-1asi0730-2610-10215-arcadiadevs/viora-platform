using ArcadiaDevs.Viora.Platform.Agronomic.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
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
///     REST controller that exposes IoT device endpoints scoped to a plot.
/// </summary>
/// <remarks>
///     (TS012TASK005) <c>GET /api/v1/plots/{plotId}/iot-devices</c> — lists all devices belonging to the specified plot.
///     (TS013TASK004) <c>POST /api/v1/plots/{plotId}/iot-devices</c> — registers a new IoT device under the specified plot.
///     (TS014) <c>PATCH /api/v1/plots/{plotId}/iot-devices/{deviceId}</c> — updates device name and status.
/// </remarks>
[ApiController]
[Route("api/v1/plots/{plotId:int}/iot-devices")]
[Authorize]
public class PlotIoTDevicesController(
    IIoTDeviceQueryService ioTDeviceQueryService,
    IIoTDeviceCommandService ioTDeviceCommandService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory,
    IClock clock) : ControllerBase
{
    /// <summary>
    ///     Registers a new IoT device under the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="resource">The request body with deviceName and optional status.</param>
    /// <returns>
    ///     <c>201 Created</c> with the created <see cref="IoTDeviceResource"/>,
    ///     or <c>400 Bad Request</c>/<c>403 Forbidden</c>/<c>404 Not Found</c> on domain failure.
    /// </returns>
    [HttpPost]
    [Tags("IoTDevice")]
    [ProducesResponseType(typeof(IoTDeviceResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateIoTDevice(
        [FromRoute] int plotId,
        [FromToken] int userId,
        [FromBody] CreateIoTDeviceResource resource)
    {
        var command = CreateIoTDeviceCommandFromResourceAssembler.ToCommandFromResource(resource, plotId, userId);
        var result = await ioTDeviceCommandService.Handle(command);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            device => CreatedAtAction(
                nameof(GetIoTDevicesByPlotId),
                new { plotId = device.PlotId },
                IoTDeviceResourceFromEntityAssembler.ToResourceFromEntity(device, clock)));
    }

    /// <summary>
    ///     Updates an existing IoT device's metadata under the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="deviceId">The device identifier (path variable).</param>
    /// <param name="resource">The request body with deviceName and status.</param>
    /// <returns>
    ///     <c>200 OK</c> with the updated <see cref="IoTDeviceResource"/>,
    ///     or <c>400 Bad Request</c>/<c>403 Forbidden</c>/<c>404 Not Found</c> on failure.
    /// </returns>
    [HttpPatch("{deviceId:int}")]
    [Tags("IoTDevice")]
    [ProducesResponseType(typeof(IoTDeviceResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIoTDevice(
        [FromRoute] int plotId,
        [FromToken] int userId,
        [FromRoute] int deviceId,
        [FromBody] UpdateIoTDeviceResource resource)
    {
        var command = UpdateIoTDeviceCommandFromResourceAssembler.ToCommandFromResource(resource, plotId, userId, deviceId);

        var result = await ioTDeviceCommandService.Handle(command);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            device => Ok(IoTDeviceResourceFromEntityAssembler.ToResourceFromEntity(device, clock)));
    }

    /// <summary>
    ///     Deletes an existing IoT device from the specified plot.
    /// </summary>
    /// <param name="plotId">The plot identifier (path variable).</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="deviceId">The device identifier (path variable).</param>
    /// <returns>
    ///     <c>204 NoContent</c> if deleted successfully,
    ///     or <c>400 Bad Request</c>/<c>403 Forbidden</c>/<c>404 Not Found</c> on failure.
    /// </returns>
    [HttpDelete("{deviceId:int}")]
    [Tags("IoTDevice")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIoTDevice(
        [FromRoute] int plotId,
        [FromToken] int userId,
        [FromRoute] int deviceId)
    {
        var command = new DeleteIoTDeviceCommand(plotId, userId, deviceId);

        var result = await ioTDeviceCommandService.Handle(command);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            _ => NoContent());
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetIoTDevicesByPlotId(
        [FromRoute] int plotId,
        [FromToken] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetIoTDevicesByPlotIdQuery(plotId, userId);
        var result = await ioTDeviceQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            readouts => Ok(readouts.Select(r => r.ToResourceFromReadout())));
    }
}
