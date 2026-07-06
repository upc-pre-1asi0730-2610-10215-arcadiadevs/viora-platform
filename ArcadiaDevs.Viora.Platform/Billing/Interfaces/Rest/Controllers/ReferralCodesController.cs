using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for referral codes (REQ-REF-4).
/// </summary>
/// <remarks>
///     Explicit <see cref="RouteAttribute" /> override to
///     <c>api/v1/referrals</c> — <c>KebabCaseRouteNamingConvention</c> would
///     otherwise auto-derive <c>/referral-codes</c> from the class name. The
///     spec-locked path is <c>/referrals</c> (design gate-review correction,
///     obs #316), NOT <c>/referral-codes</c>.
/// </remarks>
[ApiController]
[Route("api/v1/referrals")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class ReferralCodesController(
    IReferralCodeQueryService referralCodeQueryService,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Gets (or creates, on first access) a user's referral code
    ///     (REQ-REF-1, REQ-REF-4).
    /// </summary>
    /// <remarks>
    ///     Idempotent side-effecting read — matches OS behavior, intentional
    ///     per REQ-REF-4 (documented/spec-locked, not a REST-purity
    ///     violation).
    /// </remarks>
    /// <param name="userId">The user id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Referral code found or created.</response>
    /// <response code="404">Unknown user (REQ-CC-2).</response>
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(ReferralCodeResource), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateReferralCode(
        [FromRoute] int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await referralCodeQueryService.Handle(
            new GetOrCreateReferralCodeByUserIdQuery(userId), cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            referralCode => Ok(ReferralCodeResourceFromEntityAssembler.ToResourceFromEntity(referralCode)));
    }
}
