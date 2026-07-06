namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;

/// <summary>
///     Query to retrieve a user's subscription (REQ-SUB-4). <c>UserId</c> is
///     direct client input and MUST be validated via <c>IIamContextFacade</c>
///     before the repository lookup (REQ-CC-2) — a separate, distinct 404
///     case from "user exists but has no subscription".
/// </summary>
public record GetSubscriptionByUserIdQuery(int UserId);