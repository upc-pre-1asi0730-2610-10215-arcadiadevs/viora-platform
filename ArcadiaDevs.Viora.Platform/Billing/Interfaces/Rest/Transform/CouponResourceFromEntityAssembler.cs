using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="CouponResource" /> from the <see cref="Coupon" />
///     aggregate.
/// </summary>
public static class CouponResourceFromEntityAssembler
{
    /// <param name="coupon">The Coupon aggregate.</param>
    /// <param name="now">
    ///     The current time (REQ-COUP-3), sourced by the caller from
    ///     <c>IClock</c> — never computed here directly.
    /// </param>
    public static CouponResource ToResourceFromEntity(Coupon coupon, DateTime now)
    {
        return new CouponResource(
            coupon.Id,
            coupon.UserId,
            coupon.Code,
            coupon.Description,
            coupon.DiscountPercent,
            coupon.ValidUntil,
            coupon.Conditions,
            coupon.IsValid(now));
    }
}