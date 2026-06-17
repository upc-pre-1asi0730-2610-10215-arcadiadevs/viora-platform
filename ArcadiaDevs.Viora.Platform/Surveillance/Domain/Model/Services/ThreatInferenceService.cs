using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Services;

public class ThreatInferenceService
{
    public EThreatType InferFromSymptoms(Symptoms symptoms)
    {
        if (symptoms == null || symptoms.Items.Count == 0)
        {
            return EThreatType.UNKNOWN;
        }

        var descriptions = symptoms.GetDescriptions().Select(d => d.ToLowerInvariant()).ToList();

        int xylellaScore = 0;
        int fruitFlyScore = 0;
        int oliveMothScore = 0;
        int peacockSpotScore = 0;

        foreach (var desc in descriptions)
        {
            if (desc.Contains("yellowing") || desc.Contains("branch drying") || desc.Contains("dieback") || desc.Contains("scorched"))
            {
                xylellaScore += 2;
            }
            if (desc.Contains("low-vigor") || desc.Contains("leaf drop"))
            {
                xylellaScore += 1;
            }

            if (desc.Contains("fruit rot") || desc.Contains("puncture") || desc.Contains("fruit drop"))
            {
                fruitFlyScore += 2;
            }
            if (desc.Contains("worm") || desc.Contains("larvae"))
            {
                fruitFlyScore += 1;
            }

            if (desc.Contains("flower web") || desc.Contains("tunneling") || desc.Contains("mining"))
            {
                oliveMothScore += 2;
            }

            if (desc.Contains("ring spot") || desc.Contains("dark spot") || desc.Contains("peacock"))
            {
                peacockSpotScore += 2;
            }
            if (desc.Contains("defoliation"))
            {
                peacockSpotScore += 1;
            }
        }

        int maxScore = Math.Max(Math.Max(xylellaScore, fruitFlyScore), Math.Max(oliveMothScore, peacockSpotScore));

        if (maxScore == 0)
        {
            return EThreatType.PEST_SYMPTOM;
        }

        if (maxScore == xylellaScore) return EThreatType.XYLELLA_RELATED;
        if (maxScore == fruitFlyScore) return EThreatType.OLIVE_FRUIT_FLY;
        if (maxScore == oliveMothScore) return EThreatType.OLIVE_MOTH;
        return EThreatType.PEACOCK_SPOT;
    }
}
