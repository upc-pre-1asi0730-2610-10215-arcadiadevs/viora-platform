namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     POST payload for <c>/api/v1/checkouts</c> (REQ-GATE-3). The caller's
///     user id is never part of this body — it's bound from the token via
///     <c>[FromToken]</c>, matching Invoices/Coupons/PaymentMethods.
///     <see cref="Interval" /> is a string on the wire (<c>MONTHLY</c>/
///     <c>ANNUAL</c>), mirroring <c>UpdateSubscriptionResource.Status</c>'s
///     string-based convention — parsed and validated by the controller.
/// </summary>
public record CreateCheckoutResource(string PlanCode, string Interval);