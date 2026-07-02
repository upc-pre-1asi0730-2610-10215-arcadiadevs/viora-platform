using ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller that exposes IoT device endpoints aggregated across all
///     plots owned by a user.
/// </summary>
/// <remarks>
///     (REQ-8) <c>GET /api/v1/iot-devices?userId=</c> — lists every IoT device
///     across all of the requesting user's plots, backing the dashboard's
///     aggregate Water Stress view. Distinct from
///     <see cref="PlotIoTDevicesController"/>, which stays scoped to a single
///     plot and is intentionally left unchanged.
/// </remarks>
[ApiController]
[Route("api/v1/iot-devices")]
[Authorize]
public class IoTDevicesQueryController(
    IIoTDeviceQueryService ioTDeviceQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Returns all IoT devices across every plot owned by the specified user.
    /// </summary>
    /// <param name="userId">The owning user identifier (query parameter).</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>
    ///     <c>200 OK</c> with a list of <see cref="IoTDeviceResource"/> (possibly
    ///     empty when the user owns no active plots), or <c>400 Bad Request</c>
    ///     when <paramref name="userId"/> is missing or non-positive.
    /// </returns>
    [HttpGet]
    [Tags("IoTDevice")]
    [ProducesResponseType(typeof(IEnumerable<IoTDeviceResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetIoTDevicesByUserId(
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        var query = new GetIoTDevicesByUserIdQuery(userId);
        var result = await ioTDeviceQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            readouts => Ok(readouts.Select(r => r.ToResourceFromReadout())));
    }
}
