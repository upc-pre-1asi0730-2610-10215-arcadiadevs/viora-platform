using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;

/// <summary>
///     Service that handles Coupon read queries (REQ-COUP-4). Returns
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>CouponsController</c> can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface ICouponQueryService
{
    /// <summary>Lists every coupon redeemed by a user (REQ-COUP-4).</summary>
    Task<Result<IEnumerable<Coupon>, Error>> Handle(GetCouponsByUserIdQuery query, CancellationToken cancellationToken = default);
}