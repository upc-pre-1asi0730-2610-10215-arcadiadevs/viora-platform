namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents the type of biological or climate threat detected.
/// </summary>
public enum EThreatType
{
    PHENOLOGICAL_RISK,
    CHILL_DEFICIT,
    CLIMATE_EXTREME,
    PEST_SYMPTOM,
    COMMUNITY_PEST,
    LOW_NDVI,
    HYDRIC_STRESS,
    XYLELLA_RELATED,
    OLIVE_FRUIT_FLY,
    OLIVE_MOTH,
    PEACOCK_SPOT,
    WATER_STRESS,
    UNKNOWN
}
