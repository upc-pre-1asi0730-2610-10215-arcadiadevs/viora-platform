using System;
using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.AdvisorValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
///     Interface for the mitigation recommendation generator.
/// </summary>
public interface IMitigationRecommendationGenerator
{
    List<MitigationRecommendation> GenerateRecommendations(AgronomicClimateRiskLevel climateRiskLevel);
}

/// <summary>
///     Generates mitigation recommendations based on climate risk level (OS parity).
/// </summary>
public class MitigationRecommendationGenerator : IMitigationRecommendationGenerator
{
    public List<MitigationRecommendation> GenerateRecommendations(AgronomicClimateRiskLevel climateRiskLevel)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return climateRiskLevel switch
        {
            AgronomicClimateRiskLevel.Low => new List<MitigationRecommendation>
            {
                new()
                {
                    ActionType = MitigationActionType.Other,
                    SuggestedInputs = new NutritionInputRecommendation("Monitor soil moisture and maintain current practices"),
                    RecommendedApplicationWindow = new TimeWindow(today, today.AddDays(7)),
                    ClimateRiskLevel = climateRiskLevel
                }
            },
            AgronomicClimateRiskLevel.Moderate => new List<MitigationRecommendation>
            {
                new()
                {
                    ActionType = MitigationActionType.Irrigation,
                    SuggestedInputs = new NutritionInputRecommendation("Increase irrigation frequency to compensate for moisture deficit"),
                    RecommendedApplicationWindow = new TimeWindow(today, today.AddDays(3)),
                    ClimateRiskLevel = climateRiskLevel
                }
            },
            AgronomicClimateRiskLevel.High => new List<MitigationRecommendation>
            {
                new()
                {
                    ActionType = MitigationActionType.Irrigation,
                    SuggestedInputs = new NutritionInputRecommendation("Implement emergency irrigation schedule, target 120% of normal volume"),
                    RecommendedApplicationWindow = new TimeWindow(today, today.AddDays(1)),
                    ClimateRiskLevel = climateRiskLevel
                },
                new()
                {
                    ActionType = MitigationActionType.NutrientApplication,
                    SuggestedInputs = new NutritionInputRecommendation("Apply foliar nutrient spray to support stress recovery"),
                    RecommendedApplicationWindow = new TimeWindow(today.AddDays(1), today.AddDays(3)),
                    ClimateRiskLevel = climateRiskLevel
                }
            },
            AgronomicClimateRiskLevel.Extreme => new List<MitigationRecommendation>
            {
                new()
                {
                    ActionType = MitigationActionType.DiseaseControl,
                    SuggestedInputs = new NutritionInputRecommendation("Apply broad-spectrum fungicide to prevent secondary infections"),
                    RecommendedApplicationWindow = new TimeWindow(today, today.AddDays(1)),
                    ClimateRiskLevel = climateRiskLevel
                },
                new()
                {
                    ActionType = MitigationActionType.SoilTreatment,
                    SuggestedInputs = new NutritionInputRecommendation("Apply soil amendments and organic matter to improve drainage"),
                    RecommendedApplicationWindow = new TimeWindow(today.AddDays(1), today.AddDays(2)),
                    ClimateRiskLevel = climateRiskLevel
                }
            },
            AgronomicClimateRiskLevel.Unknown => new List<MitigationRecommendation>
            {
                new()
                {
                    ActionType = MitigationActionType.Other,
                    SuggestedInputs = new NutritionInputRecommendation("Conduct manual field inspection to assess crop condition"),
                    RecommendedApplicationWindow = new TimeWindow(today, today.AddDays(1)),
                    ClimateRiskLevel = climateRiskLevel
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(climateRiskLevel), climateRiskLevel, "Unknown climate risk level")
        };
    }
}
