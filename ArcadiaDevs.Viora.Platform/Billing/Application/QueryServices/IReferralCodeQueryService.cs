using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;

/// <summary>
///     Service that handles the ReferralCode get-or-create read (REQ-REF-4).
///     Returns <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>ReferralCodesController</c> can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface IReferralCodeQueryService
{
    /// <summary>
    ///     Gets (or creates, on first access) a user's referral code
    ///     (REQ-REF-1, REQ-REF-4).
    /// </summary>
    Task<Result<ReferralCode, Error>> Handle(GetOrCreateReferralCodeByUserIdQuery query, CancellationToken cancellationToken = default);
}