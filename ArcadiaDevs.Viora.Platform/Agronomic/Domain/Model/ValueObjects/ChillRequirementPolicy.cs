namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

public record ChillRequirementPolicy
{
    public double DefaultRequirementPortions { get; }
    public IReadOnlyDictionary<string, double> CropRequirementPortions { get; }

    public ChillRequirementPolicy(double defaultRequirementPortions, IDictionary<string, double>? cropRequirementPortions = null)
    {
        if (defaultRequirementPortions <= 0)
        {
            throw new ArgumentException("Default chill requirement must be positive.", nameof(defaultRequirementPortions));
        }

        DefaultRequirementPortions = defaultRequirementPortions;
        CropRequirementPortions = Normalize(cropRequirementPortions);
    }

    public ChillRequirement ResolveFor(string? cropType)
    {
        var key = string.IsNullOrWhiteSpace(cropType) ? "" : cropType.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(key) && CropRequirementPortions.TryGetValue(key, out var cropValue))
        {
            return new ChillRequirement(new ChillPortions(cropValue), EChillRequirementSource.SystemDefault, EChillMetricModel.Dynamic);
        }

        return new ChillRequirement(new ChillPortions(DefaultRequirementPortions), EChillRequirementSource.NotConfigured, EChillMetricModel.Dynamic);
    }

    private static IReadOnlyDictionary<string, double> Normalize(IDictionary<string, double>? raw)
    {
        if (raw == null || raw.Count == 0) return new Dictionary<string, double>();

        var normalized = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in raw)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Key) && kvp.Value > 0)
            {
                normalized[kvp.Key.Trim().ToLowerInvariant()] = kvp.Value;
            }
        }
        return normalized;
    }
}
