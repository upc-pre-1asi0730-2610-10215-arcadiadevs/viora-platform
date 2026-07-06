namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;

/// <summary>
///     Query to get (or create, on first access) a user's referral code
///     (REQ-REF-4). <c>UserId</c> is direct client input on this read
///     endpoint. Deliberately an idempotent side-effecting read — matches OS
///     behavior, intentional per REQ-REF-4 (documented/spec-locked, not a
///     REST-purity violation).
/// </summary>
public record GetOrCreateReferralCodeByUserIdQuery(int UserId);