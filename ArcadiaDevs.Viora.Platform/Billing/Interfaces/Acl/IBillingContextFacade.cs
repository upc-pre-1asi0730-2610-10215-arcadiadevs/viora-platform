namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Acl;

/// <summary>
///     ACL facade for cross-boundary referral-code primitives (REQ-REF-5).
/// </summary>
/// <remarks>
///     Iam's signup flow calls <see cref="GrantReferralRewardToCodeOwner" />
///     after user persistence to grant a referral reward to the referral
///     code's OWNER (REQ-REF-6) — not the new signer who supplied the code.
///     The interface lives under <c>Billing/Interfaces/Acl</c> so consuming
///     bounded contexts (Iam) depend only on this contract, not on Billing's
///     internal aggregates/repositories — mirrors
///     <c>Profile.Interfaces.Acl.IProfileContextFacade</c>'s owning-BC ACL
///     pattern (design's Cross-BC Integration section).
/// </remarks>
public interface IBillingContextFacade
{
    /// <summary>
    ///     Checks whether the given referral code exists (REQ-REF-5).
    /// </summary>
    /// <param name="code">The referral code to check.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns><c>true</c> if a referral code with this value exists; otherwise <c>false</c>.</returns>
    Task<bool> ReferralCodeExists(string? code, CancellationToken ct = default);

    /// <summary>
    ///     Grants the referral reward to the referral code's OWNER — REQ-REF-5,
    ///     REQ-REF-6. Lenient no-op if <paramref name="referralCode" /> is
    ///     blank or does not resolve to an existing code (mirrors OS: an
    ///     invalid/unknown code MUST NOT block signup). Not called for the
    ///     new signer themselves — they receive no reward from this action.
    /// </summary>
    /// <param name="referralCode">The referral code supplied at signup, if any.</param>
    /// <param name="newUserId">
    ///     The id of the newly-signed-up user who supplied the code. Used only
    ///     to build the reward coupon's unique <c>REFERRAL-REWARD-{newUserId}</c>
    ///     code — the reward itself is granted to the code's owner, not this user.
    /// </param>
    /// <param name="ct">The cancellation token.</param>
    Task GrantReferralRewardToCodeOwner(string? referralCode, int newUserId, CancellationToken ct = default);
}
