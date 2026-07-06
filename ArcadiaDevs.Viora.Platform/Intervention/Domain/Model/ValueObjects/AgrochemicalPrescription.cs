namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Immutable value object representing the agrochemical treatment
///     issued for a <see cref="Aggregates.TreatmentPrescription" />
///     (REQ-TP-3). Issued exactly once, transitioning the prescription
///     from <c>INSPECTED</c> to <c>PRESCRIBED</c>. Structured fields
///     mirror OS's <c>AgrochemicalPrescription.java</c> exactly — this was
///     previously collapsed to opaque strings in WA, a field-level parity
///     regression fixed 2026-07-05.
/// </summary>
public record AgrochemicalPrescription
{
    public ApplicationMethod ApplicationMethod { get; private set; }

    public SprayVolume SprayVolume { get; private set; } = null!;

    public PreHarvestInterval PreHarvestInterval { get; private set; } = null!;

    public string AgronomistRecommendations { get; private set; } = string.Empty;

    public IReadOnlyList<PersonalProtectiveEquipment> RequiredPPE { get; private set; } = Array.Empty<PersonalProtectiveEquipment>();

    public IReadOnlyList<PrescribedProduct> Products { get; private set; } = Array.Empty<PrescribedProduct>();

    /// <summary>
    ///     Parameterless constructor required for EF Core to materialize this
    ///     owned type, which itself owns further nested owned types
    ///     (<see cref="SprayVolume" />/<see cref="PreHarvestInterval" />) — EF
    ///     cannot bind nested owned navigations through a multi-parameter
    ///     constructor, so materialization goes through property setters
    ///     instead (mirrors the private-setter pattern used by <c>User</c>/
    ///     <c>Profile</c> elsewhere in this codebase).
    /// </summary>
    private AgrochemicalPrescription()
    {
    }

    public AgrochemicalPrescription(
        ApplicationMethod applicationMethod,
        SprayVolume sprayVolume,
        PreHarvestInterval preHarvestInterval,
        string agronomistRecommendations,
        IReadOnlyList<PersonalProtectiveEquipment>? requiredPPE,
        IReadOnlyList<PrescribedProduct> products)
    {
        ArgumentNullException.ThrowIfNull(sprayVolume);
        ArgumentNullException.ThrowIfNull(preHarvestInterval);

        if (string.IsNullOrWhiteSpace(agronomistRecommendations))
        {
            throw new ArgumentException("Agronomist recommendations are required.", nameof(agronomistRecommendations));
        }

        if (products is null || products.Count == 0)
        {
            throw new ArgumentException("At least one product must be prescribed.", nameof(products));
        }

        ApplicationMethod = applicationMethod;
        SprayVolume = sprayVolume;
        PreHarvestInterval = preHarvestInterval;
        AgronomistRecommendations = agronomistRecommendations;
        RequiredPPE = requiredPPE ?? Array.Empty<PersonalProtectiveEquipment>();
        Products = products;
    }
}
