namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for nutrient details in a nutrition plan.
/// </summary>
public record NutrientResource
{
    /// <summary>Name of the nutrient (Nitrogen, Phosphorus, or Potassium).</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Target amount.</summary>
    public decimal RequiredAmount { get; init; }

    /// <summary>Current amount.</summary>
    public decimal CurrentAmount { get; init; }

    /// <summary>Measurement unit.</summary>
    public string Unit { get; init; } = string.Empty;
}
