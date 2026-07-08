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
