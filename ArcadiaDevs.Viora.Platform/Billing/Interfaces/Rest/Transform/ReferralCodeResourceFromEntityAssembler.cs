using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="ReferralCodeResource" /> from the
///     <see cref="ReferralCode" /> aggregate.
/// </summary>
public static class ReferralCodeResourceFromEntityAssembler
{
    public static ReferralCodeResource ToResourceFromEntity(ReferralCode referralCode)
    {
        return new ReferralCodeResource(
            referralCode.Id,
            referralCode.UserId,
            referralCode.Code,
            ReferralCode.RewardPercent);
    }
}