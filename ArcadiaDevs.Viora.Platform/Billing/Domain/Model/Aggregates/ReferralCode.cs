namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;

/// <summary>
///     The ReferralCode aggregate root — a user's unique referral code
///     (REQ-REF-1..3). Both <see cref="UserId" /> and <see cref="Code" /> are
///     ctor-only immutable.
/// </summary>
/// <remarks>
///     No self-guard — this aggregate has no status field and no state
///     transitions. <see cref="RewardPercent" /> is a fixed business constant
///     (REQ-REF-3), not a persisted per-instance field, so it needs no EF
///     mapping. Get-or-create idempotency (REQ-REF-1) is a repository-lookup
///     concern handled by <c>ReferralCodeCommandService</c>, not an aggregate
///     transition — mirrors the design's Per-Aggregate Design table.
/// </remarks>
public class ReferralCode
{
    /// <summary>REQ-REF-3: fixed reward percentage granted to the code's owner.</summary>
    public const int RewardPercent = 20;

    public int Id { get; }

    public int UserId { get; }

    public string Code { get; }

    private ReferralCode()
    {
        Code = string.Empty;
    }

    public ReferralCode(int userId, string code)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be positive.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        UserId = userId;
        Code = code;
    }
}