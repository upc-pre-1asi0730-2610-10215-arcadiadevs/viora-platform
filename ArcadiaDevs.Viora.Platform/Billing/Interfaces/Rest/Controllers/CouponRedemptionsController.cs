using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

/// <summary>
///     REST controller for coupon redemptions (REQ-COUP-2). Noun-resource
///     route (<c>/coupon-redemptions</c>) — NOT <c>POST /coupons/redeem</c>,
///     which would be a verb-in-URL violation (REQ-CC-1).
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class CouponRedemptionsController(
    ICouponCommandService couponCommandService,
    IClock clock,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    /// <summary>
    ///     Redeems a catalog code for a user (REQ-COUP-2).
    /// </summary>
    /// <param name="resource">The redemption payload (<c>code</c>).</param>
    /// <param name="userId">The authenticated caller's id, derived from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Coupon redeemed.</response>
    /// <response code="404">Unknown user, or unknown catalog code.</response>
    /// <response code="409">That user already redeemed this code (REQ-COUP-2).</response>
    [HttpPost]
    [ProducesResponseType(typeof(CouponResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RedeemCoupon(
        [FromBody] RedeemCouponResource resource,
        [FromToken] int userId,
        CancellationToken cancellationToken = default)
    {
        var command = new RedeemCouponCommand(userId, resource.Code);
        var result = await couponCommandService.Handle(command, cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            coupon => StatusCode(
                StatusCodes.Status201Created,
                CouponResourceFromEntityAssembler.ToResourceFromEntity(coupon, clock.UtcNow)));
    }
}
