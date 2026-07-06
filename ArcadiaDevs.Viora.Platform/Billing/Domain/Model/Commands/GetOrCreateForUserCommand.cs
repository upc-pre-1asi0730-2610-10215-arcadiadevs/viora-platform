namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;

/// <summary>
///     Command to get (or create, on first access) a user's referral code
///     (REQ-REF-1). <c>UserId</c> is direct client input and MUST be
///     validated via <c>IIamContextFacade</c> before persisting (REQ-CC-2).
///     Idempotent — repeated calls for the same user return the same code,
///     never a second row.
/// </summary>
public record GetOrCreateForUserCommand(int UserId);