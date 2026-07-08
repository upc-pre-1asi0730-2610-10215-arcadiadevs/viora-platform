namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

public record ReferralCodeResource(int Id, int UserId, string Code, int RewardPercent);