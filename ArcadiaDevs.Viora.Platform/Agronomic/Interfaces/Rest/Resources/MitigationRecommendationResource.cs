namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

/// <summary>
///     Resource for a mitigation recommendation from the advisor.
/// </summary>
public record MitigationRecommendationResource
{
    public string ActionType { get; init; } = string.Empty;
    public string SuggestedInputs { get; init; } = string.Empty;
    public string RecommendedApplicationWindow { get; init; } = string.Empty;
    public string ClimateRiskLevel { get; init; } = string.Empty;
}
