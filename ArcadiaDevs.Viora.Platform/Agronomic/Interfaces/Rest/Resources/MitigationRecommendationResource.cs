namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

public record MitigationRecommendationResource
{
    public string ActionType { get; init; } = string.Empty;
    public string SuggestedInputs { get; init; } = string.Empty;
    public string RecommendedApplicationWindow { get; init; } = string.Empty;
    public string ClimateRiskLevel { get; init; } = string.Empty;
}
