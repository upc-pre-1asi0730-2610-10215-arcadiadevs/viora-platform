namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;

/// <summary>
///     The Coupon aggregate root — a per-user redemption record for a
///     <see cref="Services.CouponCatalog" /> template (REQ-COUP-1..4). Every
///     field is ctor-only immutable.
/// </summary>
/// <remarks>
///     No self-guard — this aggregate has no status field (anemic-by-nature,
///     matches OS). Redemption idempotency is a command-service-level guard
///     (REQ-COUP-2, 409 via <c>ICouponRepository.ExistsByUserIdAndCodeAsync</c>),
///     not an aggregate transition — mirrors the design's Per-Aggregate
///     Design table.
/// </remarks>
public class Coupon
{
    public int Id { get; }

    public int UserId { get; }

    public string Code { get; }

    public string Description { get; }

    public int DiscountPercent { get; }

    /// <summary>Nullable expiry — <c>null</c> means the coupon never expires (REQ-COUP-3).</summary>
    public DateTime? ValidUntil { get; }

    public string Conditions { get; }

    private Coupon()
    {
        Code = string.Empty;
        Description = string.Empty;
        Conditions = string.Empty;
    }

    public Coupon(
        int userId,
        string code,
        string description,
        int discountPercent,
        DateTime? validUntil,
        string conditions)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be positive.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        if (discountPercent is <= 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercent), discountPercent, "Discount percent must be between 1 and 100.");
        }

        UserId = userId;
        Code = code;
        Description = description ?? string.Empty;
        DiscountPercent = discountPercent;
        ValidUntil = validUntil;
        Conditions = conditions ?? string.Empty;
    }

    /// <summary>
    ///     REQ-COUP-3: <see langword="true" /> when <see cref="ValidUntil" /> is
    ///     <see langword="null" /> or still in the future relative to
    ///     <paramref name="now" />. Takes the current time as a parameter
    ///     (caller sources it from <c>IClock</c>) instead of calling
    ///     <c>DateTime.UtcNow</c>/<c>DateTimeOffset.UtcNow</c> directly, so the
    ///     aggregate stays deterministic and testable (SHARED-008).
    /// </summary>
    public bool IsValid(DateTime now) => ValidUntil is null || ValidUntil.Value > now;
}
