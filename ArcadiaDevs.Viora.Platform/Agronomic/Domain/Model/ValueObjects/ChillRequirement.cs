namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     A plot's winter-chill requirement.
/// </summary>
public record ChillRequirement
{
    public ChillPortions Portions { get; init; }
    public EChillRequirementSource Source { get; init; }
    public EChillMetricModel Model { get; init; }

    public ChillRequirement(ChillPortions portions, EChillRequirementSource source, EChillMetricModel model)
    {
        ArgumentNullException.ThrowIfNull(portions);

        if (portions.Value <= 0)
        {
            throw new ArgumentException("Chill requirement must be positive.");
        }

        Portions = portions;
        Source = source;
        Model = model;
    }
    
    /// <summary>
    /// For EF Core mapping
    /// </summary>
    protected ChillRequirement()
    {
        Portions = null!;
    }
}
