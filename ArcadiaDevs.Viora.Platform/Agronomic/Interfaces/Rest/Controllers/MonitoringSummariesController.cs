using ArcadiaDevs.Viora.Platform.Agronomic.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/monitoring-summaries")]
[Authorize]
public class MonitoringSummariesController(
    IMonitoringSummaryQueryService monitoringSummaryQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(MonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCurrent(
        [FromToken] int userId,
        [FromQuery] int limit = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new GetCurrentMonitoringSummaryQuery(userId);
        var result = await monitoringSummaryQueryService.Handle(query, cancellationToken);

        return AgronomicActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            summary => Ok(summary));
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(MonitoringSummaryResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetCurrentLegacy(
        [FromToken] int userId,
        [FromQuery] int limit = 1,
        CancellationToken cancellationToken = default) =>
        GetCurrent(userId, limit, cancellationToken);
}
