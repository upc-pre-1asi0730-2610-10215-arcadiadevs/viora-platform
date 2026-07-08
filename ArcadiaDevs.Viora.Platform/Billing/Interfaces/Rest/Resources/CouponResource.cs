namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

public record CouponResource(
    int Id,
    int UserId,
    string Code,
    string Description,
    int DiscountPercent,
    DateTime? ValidUntil,
    string Conditions,
    bool IsValid);