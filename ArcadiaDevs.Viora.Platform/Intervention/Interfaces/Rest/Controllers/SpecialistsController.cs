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

/// <summary>
///     REST controller for specialist public profile and gated contact
///     (REQ-SPEC-1, REQ-SPEC-2).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistsController(
    ISpecialistQueryService specialistQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Get a specialist's public profile.
    /// </summary>
    /// <param name="id">The specialist's <c>ProfileUserId</c> (not the EF <c>Specialist.Id</c> PK — specialists are resolved as a projection over <c>Profile</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Public profile returned (no contact fields).</response>
    /// <response code="404">Specialist not found.</response>
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

    /// <summary>
    ///     Get a specialist's gated contact info.
    /// </summary>
    /// <remarks>
    ///     Only unlocked if the referenced intervention request is
    ///     <c>ACCEPTED</c>, matches the requested specialist, AND the
    ///     authenticated caller owns the request (REQ-SPEC-2, WU1 fix pass
    ///     item #10) — status+specialist-id matching alone is not
    ///     sufficient to authorize the caller. The caller id is read from
    ///     the <c>sid</c>/<c>sub</c> claim populated by
    ///     <c>RequestAuthorizationMiddleware</c>, same pattern as
    ///     <c>UsersController.GetMe</c>.
    /// </remarks>
    /// <param name="id">The specialist's <c>ProfileUserId</c> (not the EF <c>Specialist.Id</c> PK).</param>
    /// <param name="requestId">The intervention request id gating access.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Contact info returned.</response>
    /// <response code="401">Caller identity could not be resolved from the token.</response>
    /// <response code="403">Contact not unlocked for this request.</response>
    /// <response code="404">Specialist not found.</response>
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
