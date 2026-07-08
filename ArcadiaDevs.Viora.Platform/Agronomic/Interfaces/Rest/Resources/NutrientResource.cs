namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record NutrientResource
{
    public string Name { get; init; } = string.Empty;

    public decimal RequiredAmount { get; init; }

    public decimal CurrentAmount { get; init; }

    public string Unit { get; init; } = string.Empty;
}
