using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;

/// <summary>
///     The Plan aggregate root — a read-mostly catalog entry (REQ-PLAN-3).
///     Ctor-immutable, no status field, no self-guarded transitions and no
///     FK to any other bounded context (design's Per-Aggregate Design
///     table). Seeded idempotently at startup (REQ-PLAN-1), never mutated
///     afterwards through this change's REST surface.
/// </summary>
public class Plan
{
    public int Id { get; }

    /// <summary>Unique catalog code (e.g. <c>BASIC</c>, <c>PRO</c>).</summary>
    public string Code { get; }

    public string Name { get; }

    /// <summary>
    ///     Flat decimal amount, no dedicated Money VO (design's EF Core
    ///     Design section — mirrors <c>InterventionExecution.AppliedArea</c>'s
    ///     plain-decimal precedent).
    /// </summary>
    public decimal PriceAmount { get; }

    /// <summary>
    ///     ISO currency code. Defaults to <c>PEN</c> as a ctor default
    ///     parameter (REQ-CC-4) — the single point where the aggregate can
    ///     ever come into existence.
    /// </summary>
    public string Currency { get; }

    public PlanInterval Interval { get; }

    public string Tagline { get; }

    /// <summary>
    ///     Marketing feature bullet list. JSON-converted at the EF mapping
    ///     layer, mirrors <c>AgrochemicalPrescription.Products</c>.
    /// </summary>
    public IReadOnlyList<string> Features { get; }

    public int PlotLimit { get; }

    public int IotLimit { get; }

    private Plan()
    {
        Code = string.Empty;
        Name = string.Empty;
        Currency = "PEN";
        Tagline = string.Empty;
        Features = new List<string>().AsReadOnly();
    }

    public Plan(
        string code,
        string name,
        decimal priceAmount,
        PlanInterval interval,
        string tagline,
        IReadOnlyList<string> features,
        int plotLimit,
        int iotLimit,
        string currency = "PEN")
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (priceAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(priceAmount), priceAmount, "Price amount cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        if (string.IsNullOrWhiteSpace(tagline))
        {
            throw new ArgumentException("Tagline is required.", nameof(tagline));
        }

        if (plotLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plotLimit), plotLimit, "Plot limit cannot be negative.");
        }

        if (iotLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iotLimit), iotLimit, "IoT limit cannot be negative.");
        }

        Code = code;
        Name = name;
        PriceAmount = priceAmount;
        Currency = currency;
        Interval = interval;
        Tagline = tagline;
        Features = (features ?? Enumerable.Empty<string>())
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .ToList()
            .AsReadOnly();
        PlotLimit = plotLimit;
        IotLimit = iotLimit;
    }
}
