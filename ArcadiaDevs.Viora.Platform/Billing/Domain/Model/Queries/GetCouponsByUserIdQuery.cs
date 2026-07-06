namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;

/// <summary>
///     Query to list every coupon redeemed by a user (REQ-COUP-4).
///     <c>UserId</c> is direct client input on this read endpoint and MUST
///     be validated via <c>IIamContextFacade</c> before the repository
///     lookup (REQ-CC-2).
/// </summary>
public record GetCouponsByUserIdQuery(int UserId);