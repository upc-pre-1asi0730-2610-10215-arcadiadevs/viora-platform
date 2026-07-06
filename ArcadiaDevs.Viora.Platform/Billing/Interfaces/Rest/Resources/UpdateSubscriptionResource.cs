namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

/// <summary>
///     PATCH payload for <c>/api/v1/subscriptions/{userId}</c> (REQ-SUB-2).
///     The only supported target <c>Status</c> value is <c>CANCELED</c> —
///     plan-switch stays internal-only (REQ-SUB-3), never exposed via this
///     resource.
/// </summary>
public record UpdateSubscriptionResource(string Status);