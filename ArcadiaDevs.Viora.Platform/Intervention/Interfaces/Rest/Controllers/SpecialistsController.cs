using System.Net.Mime;
using System.Security.Claims;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistsController(
    ISpecialistQueryService specialistQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SpecialistResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpecialistById(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        var result = await specialistQueryService.Handle(new GetSpecialistByIdQuery(id), cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            dto => Ok(SpecialistResourceFromDtoAssembler.ToResourceFromDto(dto)));
    }

    [HttpGet("{id:int}/contact")]
    [ProducesResponseType(typeof(SpecialistContactResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpecialistContact(
        [FromRoute] int id,
        [FromQuery] int requestId,
        CancellationToken cancellationToken = default)
    {
        var sub = User.FindFirstValue(ClaimTypes.Sid) ?? User.FindFirstValue("sub");
        if (!int.TryParse(sub, out var callerUserId))
        {
            var unauthorized = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status401Unauthorized,
                IamErrors.TokenRequired.Code,
                errorLocalizer[IamErrors.TokenRequired.Code].Value ?? IamErrors.TokenRequired.Message);
            return Unauthorized(unauthorized);
        }

        var result = await specialistQueryService.Handle(
            new GetSpecialistContactQuery(id, requestId, callerUserId), cancellationToken);

        return InterventionActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            dto => Ok(SpecialistResourceFromDtoAssembler.ToResourceFromDto(dto)));
    }
}
