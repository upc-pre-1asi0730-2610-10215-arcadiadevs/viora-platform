namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;

/// <summary>
///     The PaymentMethod aggregate root — display metadata for a user's
///     saved card, revealed only through an approved payment (REQ-PM-1..3).
///     <see cref="UserId" /> is ctor-only immutable; the card display fields
///     are mutable ONLY through <see cref="ReplaceCardMetadata" />, the
///     upsert mechanic invoked by WU6's webhook reconciliation (design's
///     Per-Aggregate Design table: "find-by-UserId then ctor-replace via
///     repo Update").
/// </summary>
/// <remarks>
///     No self-guard — this aggregate has no status field and no guarded
///     transitions (REQ-CC-3's status-transition guard summary explicitly
///     excludes PaymentMethod). It MUST NOT ever persist full card numbers,
///     CVV, or any PCI-scoped data — only brand/last4/expiry/isDefault
///     display metadata (REQ-PM-1).
/// </remarks>
public class PaymentMethod
{
    public int Id { get; }

    public int UserId { get; }

    /// <summary>Card network brand (e.g. <c>VISA</c>, <c>MASTERCARD</c>).</summary>
    public string Brand { get; private set; }

    /// <summary>Last 4 digits of the card number — display only, never the full PAN.</summary>
    public string Last4 { get; private set; }

    public int ExpMonth { get; private set; }

    public int ExpYear { get; private set; }

    public bool IsDefault { get; private set; }

    private PaymentMethod()
    {
        Brand = string.Empty;
        Last4 = string.Empty;
    }

    public PaymentMethod(
        int userId,
        string brand,
        string last4,
        int expMonth,
        int expYear,
        bool isDefault)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("User ID must be positive.", nameof(userId));
        }

        UserId = userId;
        ReplaceCardMetadata(brand, last4, expMonth, expYear, isDefault);
    }

    /// <summary>
    ///     Replaces this payment method's display metadata in place (REQ-PM-2
    ///     upsert). Re-runs the exact same validation the constructor
    ///     applies ("ctor-replace" semantics), then the caller persists the
    ///     change on the already-tracked existing row via
    ///     <c>IBaseRepository{TEntity}.Update</c> — no new row is created and
    ///     the original database <see cref="Id" /> is preserved, satisfying
    ///     REQ-PM-2's "single row per user" upsert guarantee.
    /// </summary>
    public PaymentMethod ReplaceCardMetadata(
        string brand,
        string last4,
        int expMonth,
        int expYear,
        bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException("Brand is required.", nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4 || !last4.All(char.IsDigit))
        {
            throw new ArgumentException("Last4 must be exactly 4 digits.", nameof(last4));
        }

        if (expMonth is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(expMonth), expMonth, "Expiry month must be between 1 and 12.");
        }

        if (expYear <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expYear), expYear, "Expiry year must be positive.");
        }

        Brand = brand;
        Last4 = last4;
        ExpMonth = expMonth;
        ExpYear = expYear;
        IsDefault = isDefault;

        return this;
    }
}
