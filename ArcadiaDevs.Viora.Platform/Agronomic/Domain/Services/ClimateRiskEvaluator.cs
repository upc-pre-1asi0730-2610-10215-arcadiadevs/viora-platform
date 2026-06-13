using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Services;

/// <summary>
/// Domain service to evaluate climate risks and generate mitigation recommendations.
/// </summary>
public class ClimateRiskEvaluator
{
    /// <summary>
    /// Evaluates the consolidated climate risk by combining weather data, NDVI, and chill hours.
    /// </summary>
    /// <returns>A tuple containing the final calculated risk and a corresponding recommendation.</returns>
    public (ClimateRiskLevel, MitigationRecommendation) EvaluateRisk(
        AccumulatedChillHours chillHours,
        AverageNdvi ndvi,
        WeatherSnapshot weather)
    {
        // Prioritize immediate weather-based risks
        if (weather.ClimateRiskLevel is ClimateRiskLevel.High or ClimateRiskLevel.Critical)
        {
            var recommendation = new MitigationRecommendation(
                "Immediate Action Required",
                "Evaluate frost/heat protection measures",
                "Next 24 hours",
                weather.ClimateRiskLevel);
            return (weather.ClimateRiskLevel, recommendation);
        }
        
        // Evaluate agronomic factors for medium risk
        if (chillHours.Value < 200 && ndvi.Value < 0.4m)
        {
            var risk = ClimateRiskLevel.Medium;
            var recommendation = new MitigationRecommendation(
                "Nutrition Boost",
                "Nitrogen fertilizer",
                "Next 7 days",
                risk);
            return (risk, recommendation);
        }

        // If no other risks are found, the risk is low.
        return (ClimateRiskLevel.Low, MitigationRecommendation.None());
    }
}