namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Immutable value object representing the agrochemical treatment
///     issued for a <see cref="Aggregates.TreatmentPrescription" />
///     (REQ-TP-3). Issued exactly once, transitioning the prescription
///     from <c>INSPECTED</c> to <c>PRESCRIBED</c>.
/// </summary>
public record AgrochemicalPrescription
{
    public string ApplicationMethod { get; }

    public string SprayVolume { get; }

    public string PreHarvestInterval { get; }

    public string AgronomistRecommendations { get; }

    public string RequiredPPE { get; }

    public IReadOnlyList<string> Products { get; }

    public AgrochemicalPrescription(
        string applicationMethod,
        string sprayVolume,
        string preHarvestInterval,
        string agronomistRecommendations,
        string requiredPPE,
        IEnumerable<string> products)
    {
        if (string.IsNullOrWhiteSpace(applicationMethod))
        {
            throw new ArgumentException("Application method is required.", nameof(applicationMethod));
        }

        if (string.IsNullOrWhiteSpace(sprayVolume))
        {
            throw new ArgumentException("Spray volume is required.", nameof(sprayVolume));
        }

        if (string.IsNullOrWhiteSpace(preHarvestInterval))
        {
            throw new ArgumentException("Pre-harvest interval is required.", nameof(preHarvestInterval));
        }

        if (string.IsNullOrWhiteSpace(agronomistRecommendations))
        {
            throw new ArgumentException("Agronomist recommendations are required.", nameof(agronomistRecommendations));
        }

        if (string.IsNullOrWhiteSpace(requiredPPE))
        {
            throw new ArgumentException("Required PPE is required.", nameof(requiredPPE));
        }

        var productList = (products ?? Enumerable.Empty<string>())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();

        if (productList.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(products));
        }

        ApplicationMethod = applicationMethod;
        SprayVolume = sprayVolume;
        PreHarvestInterval = preHarvestInterval;
        AgronomistRecommendations = agronomistRecommendations;
        RequiredPPE = requiredPPE;
        Products = productList.AsReadOnly();
    }
}
