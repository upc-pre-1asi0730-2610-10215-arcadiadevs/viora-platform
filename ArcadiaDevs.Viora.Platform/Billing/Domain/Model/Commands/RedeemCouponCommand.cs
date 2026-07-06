namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to redeem a <see cref="Services.CouponCatalog" /> code for a
///     user (REQ-COUP-2). <c>UserId</c> is direct client input and MUST be
///     validated via <c>IIamContextFacade</c> before persisting (REQ-CC-2).
/// </summary>
public record RedeemCouponCommand(int UserId, string Code);