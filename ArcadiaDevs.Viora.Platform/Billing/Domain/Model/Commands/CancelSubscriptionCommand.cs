namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to cancel a user's subscription (REQ-SUB-2). Self-guarded on
///     the aggregate — fails with a <c>ConflictError</c> (409) unless the
///     subscription is currently <c>ACTIVE</c>. Invoked from
///     <c>PATCH /api/v1/subscriptions/{userId}</c> with
///     <c>{"status":"CANCELED"}</c>.
/// </summary>
public record CancelSubscriptionCommand(int UserId);