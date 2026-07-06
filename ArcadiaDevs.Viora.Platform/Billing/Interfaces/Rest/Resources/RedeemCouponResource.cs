namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Request body for <c>POST /coupon-redemptions</c> (REQ-COUP-2).
/// </summary>
public record RedeemCouponResource(int UserId, string Code);