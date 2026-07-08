using System.Net.Mime;
using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Authorize]
public class CouponsController(
    ICouponQueryService couponQueryService,
    IClock clock,
    IStringLocalizer<ErrorMessages> errorLocalizer,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CouponResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCouponsByUserId(
        [FromToken] int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await couponQueryService.Handle(new GetCouponsByUserIdQuery(userId), cancellationToken);

        return BillingActionResultAssembler.ToActionResult(
            this,
            result,
            errorLocalizer,
            problemDetailsFactory,
            coupons => Ok(coupons
                .Select(coupon => CouponResourceFromEntityAssembler.ToResourceFromEntity(coupon, clock.UtcNow))
                .ToList()));
    }
}
