using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

/// <summary>
/// Domain service to evaluate climate risks and generate mitigation recommendations.
/// </summary>
public class ClimateRiskEvaluator
{
    public MitigationRecommendation EvaluateRisk(
        AccumulatedChillHours chillHours,
        AverageNdvi ndvi,
        WeatherSnapshot weather)
    {
        // Simple logic for Olive tree (olivo)
        if (weather.ClimateRiskLevel == ClimateRiskLevel.Critical || weather.ClimateRiskLevel == ClimateRiskLevel.High)
        {
            return new MitigationRecommendation(
                "Immediate Action Required",
                "Fungicide / Frost protection",
                "Next 24 hours",
                weather.ClimateRiskLevel);
        }
        
        if (chillHours.Value < 200 && ndvi.Value < 0.4m)
        {
            return new MitigationRecommendation(
                "Nutrition Boost",
                "Nitrogen fertilizer",
                "Next 7 days",
                ClimateRiskLevel.Medium);
        }

        // Return empty if low risk
        return MitigationRecommendation.None();
    }
}
