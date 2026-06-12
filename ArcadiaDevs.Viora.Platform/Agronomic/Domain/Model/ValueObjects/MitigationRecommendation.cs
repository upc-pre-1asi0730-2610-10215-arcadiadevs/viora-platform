using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

/// <summary>
///     Mitigation recommendation value object.
/// </summary>
public record MitigationRecommendation
{
    public string ActionType { get; init; }
    public string SuggestedInputs { get; init; }
    public string RecommendedApplicationWindow { get; init; }

    public MitigationRecommendation(
        string actionType, 
        string suggestedInputs, 
        string recommendedApplicationWindow,
        ClimateRiskLevel riskLevel)
    {
        if (riskLevel == ClimateRiskLevel.Low && 
            (!string.IsNullOrEmpty(actionType) || !string.IsNullOrEmpty(suggestedInputs)))
        {
            throw new ArgumentException("Mitigation recommendations should only be generated for Medium, High or Critical risks.");
        }

        ActionType = actionType ?? string.Empty;
        SuggestedInputs = suggestedInputs ?? string.Empty;
        RecommendedApplicationWindow = recommendedApplicationWindow ?? string.Empty;
    }

    public static MitigationRecommendation None() => new(string.Empty, string.Empty, string.Empty, ClimateRiskLevel.Low);
}
