using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="Coupon" />
///     (REQ-COUP-2). Returns <see cref="Result{TValue, TError}" /> (REQ-CC-3)
///     so <c>CouponRedemptionsController</c> can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface ICouponCommandService
{
    /// <summary>Redeems a catalog code for a user (REQ-COUP-2).</summary>
    Task<Result<Coupon, Error>> Handle(RedeemCouponCommand command, CancellationToken cancellationToken = default);
}