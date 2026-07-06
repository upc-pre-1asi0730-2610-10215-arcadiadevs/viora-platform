namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     Coupon resource (REQ-COUP-3, REQ-COUP-4).
/// </summary>
public record CouponResource(
    int Id,
    int UserId,
    string Code,
    string Description,
    int DiscountPercent,
    DateTime? ValidUntil,
    string Conditions,
    bool IsValid);