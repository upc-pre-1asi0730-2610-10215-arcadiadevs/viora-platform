namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     POST payload for <c>/api/v1/checkouts</c> (REQ-GATE-3).
///     <see cref="Interval" /> is a string on the wire (<c>MONTHLY</c>/
///     <c>ANNUAL</c>), mirroring <c>UpdateSubscriptionResource.Status</c>'s
///     string-based convention — parsed and validated by the controller.
/// </summary>
public record CreateCheckoutResource(int UserId, string PlanCode, string Interval);