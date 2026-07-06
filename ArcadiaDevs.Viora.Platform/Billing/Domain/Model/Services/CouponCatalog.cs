namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Services;

/// <summary>
///     Static in-code catalog of redeemable coupon codes (REQ-COUP-1), NOT
///     DB-backed — locked parity with OS. Placement as a domain service
///     mirrors <c>SpecialistMatchingPolicy</c>'s folder convention, though
///     unlike that class this catalog needs no repository/DI at all: it is a
///     pure static lookup consumed directly by <c>CouponCommandService</c>.
/// </summary>
public static class CouponCatalog
{
    /// <summary>A redeemable coupon template (REQ-COUP-1).</summary>
    public record Template(int DiscountPercent, int ValidityDays, string Description, string Conditions);

    private static readonly IReadOnlyDictionary<string, Template> Templates = new Dictionary<string, Template>
    {
        ["REFERAL20"] = new Template(
            DiscountPercent: 20,
            ValidityDays: 28,
            Description: "Referral reward — 20% off your next billing cycle.",
            Conditions: "Granted once per successful referral; combines with no other coupon."),
        ["WELCOME10"] = new Template(
            DiscountPercent: 10,
            ValidityDays: 28,
            Description: "Welcome discount — 10% off your first billing cycle.",
            Conditions: "New subscribers only; single use per account.")
    };

    /// <summary>
    ///     Looks up a coupon template by its code (REQ-COUP-2 — 404 when
    ///     unknown, resolved by the caller).
    /// </summary>
    public static bool TryGetTemplate(string code, out Template template)
    {
        return Templates.TryGetValue(code, out template!);
    }
}