using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Intervention.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Intervention.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for specialist candidate matching (REQ-SPEC-3).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class SpecialistCandidatesController(
    ISpecialistQueryService specialistQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     The default number of candidates returned when <c>limit</c> is
    ///     omitted from the query string. Single source of truth — the
    ///     domain query record and matching policy no longer duplicate this
    ///     default since this controller is the only caller and always
    ///     passes an explicit value down the chain.
    /// </summary>
    public const int DefaultCandidateLimit = 3;

    /// <summary>
    ///     The maximum <c>limit</c> value accepted, regardless of what the
    ///     caller requests — protects against loading the full specialists
    ///     table before client-side truncation.
    /// </summary>
    public const int MaxCandidateLimit = 50;

    /// <summary>
    ///     Get ranked specialist candidates for an alert.
    /// </summary>
    /// <remarks>
    ///     Ranks live <c>Profile</c> data via <see cref="SpecialistMatchingPolicy" />:
    ///     in-radius specialists first (by tag relevance → availability →
    ///     distance), then out-of-radius specialists as fallback (by
    ///     distance → relevance → availability), capped to
    ///     <paramref name="limit" /> (default <see cref="DefaultCandidateLimit" />,
    ///     max <see cref="MaxCandidateLimit" />). <paramref name="alertId" />
    ///     resolves the alert's plot + threat type (via the Surveillance
    ///     ACL) that drives distance and tag-relevance ranking; when it does
    ///     not resolve, results fall back to availability-only ranking.
    /// </remarks>
    /// <param name="alertId">Reserved parameter, currently unused for ranking.</param>
    /// <param name="limit">The maximum number of candidates to return (default 3, capped at 50).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Ranked candidates returned (possibly empty).</response>
    /// <response code="400"><paramref name="limit" /> is not positive.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpecialistResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSpecialistCandidates(
        [FromQuery] long? alertId,
        [FromQuery] int limit = DefaultCandidateLimit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                HttpContext,
                StatusCodes.Status400BadRequest,
                InterventionErrors.ValidationError.Code,
                errorLocalizer[InterventionErrors.ValidationError.Code].Value ?? InterventionErrors.ValidationError.Message);
            return BadRequest(problemDetails);
        }

        var effectiveLimit = Math.Min(limit, MaxCandidateLimit);

        var candidates = await specialistQueryService.Handle(
            new GetSpecialistCandidatesQuery(alertId, effectiveLimit), cancellationToken);

        return Ok(candidates.Select(SpecialistResourceFromDtoAssembler.ToResourceFromDto));
    }
}
