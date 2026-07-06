namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Referral code resource (REQ-REF-2..4).
/// </summary>
public record ReferralCodeResource(int Id, int UserId, string Code, int RewardPercent);