using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="ReferralCode" /> aggregates.
///     Unique index on <see cref="ReferralCode.UserId" /> (REQ-REF-1 — one
///     referral code per user) and on <see cref="ReferralCode.Code" />
///     (REQ-REF-2 — globally unique codes).
/// </summary>
public interface IReferralCodeRepository : IBaseRepository<ReferralCode>
{
    /// <summary>
    ///     Finds the (at most one) referral code belonging to the given user.
    /// </summary>
    Task<ReferralCode?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a referral code already exists for the given code —
    ///     used by the loop-until-unique generation guard (REQ-REF-2).
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Finds the referral code by its unique <see cref="ReferralCode.Code" />
    ///     value — used by <c>BillingContextFacade</c> (REQ-REF-5) to resolve
    ///     the code's owner at signup time (REQ-REF-6).
    /// </summary>
    Task<ReferralCode?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
}