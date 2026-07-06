using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Acl;

/// <summary>
///     ACL facade implementation that wraps <see cref="IReferralCodeRepository" />
///     and <see cref="ICouponRepository" /> for cross-boundary referral-reward
///     granting (REQ-REF-5, REQ-REF-6).
/// </summary>
/// <remarks>
///     Lenient by design (mirrors OS): a blank or unknown
///     <c>referralCode</c> is a silent no-op, never a failure — signup MUST
///     NOT be blocked by referral-processing issues. The reward coupon is
///     built from the same <see cref="CouponCatalog" /> template
///     (<c>REFERAL20</c>, 20%/28 days) that <c>CouponCommandService</c> uses
///     for direct redemptions, keeping the reward shape consistent across
///     both paths. No dedicated idempotency layer is needed: this method is
///     invoked exactly once per successful signup transaction (unlike the
///     payment webhook, which IS retried), and the reward coupon's
///     <c>Code</c> is <c>REFERRAL-REWARD-{newUserId}</c> — naturally unique
///     per (owner, referring new-user) pair, so repeat referrals of the same
///     code by different new users never collide against the
///     <c>(UserId, Code)</c> composite-unique index.
/// </remarks>
public class BillingContextFacade(
    IReferralCodeRepository referralCodeRepository,
    ICouponRepository couponRepository,
    IClock clock,
    IUnitOfWork unitOfWork) : IBillingContextFacade
{
    /// <summary>
    ///     Reuses the same referral-reward template <c>CouponCommandService</c>
    ///     redeems directly (20% / 28 days) — REQ-REF-5/REQ-REF-6 do not
    ///     define a separate template, so the existing catalog entry is the
    ///     natural source of truth.
    /// </summary>
    private const string ReferralRewardTemplateCode = "REFERAL20";

    /// <inheritdoc />
    public async Task<bool> ReferralCodeExists(string? code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return await referralCodeRepository.ExistsByCodeAsync(code, ct);
    }

    /// <inheritdoc />
    public async Task GrantReferralRewardToCodeOwner(string? referralCode, int newUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
        {
            return;
        }

        var owner = await referralCodeRepository.FindByCodeAsync(referralCode, ct);
        if (owner is null)
        {
            return;
        }

        if (!CouponCatalog.TryGetTemplate(ReferralRewardTemplateCode, out var template))
        {
            return;
        }

        var validUntil = clock.UtcNow.AddDays(template.ValidityDays);
        var coupon = new Coupon(
            owner.UserId,
            $"REFERRAL-REWARD-{newUserId}",
            template.Description,
            template.DiscountPercent,
            validUntil,
            template.Conditions);

        await couponRepository.AddAsync(coupon, ct);
        await unitOfWork.CompleteAsync(ct);
    }
}
