namespace ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.ValueObjects;

/// <summary>
/// Represents a predefined symptom catalog type.
/// </summary>
/// <param name="Code">The unique string code of the symptom type.</param>
/// <param name="DescriptionEn">The description in English.</param>
/// <param name="DescriptionEs">The description in Spanish.</param>
public record SymptomTypes(string Code, string DescriptionEn, string DescriptionEs)
{
    public static readonly SymptomTypes Xylella = new("XYLELLA", "Xylella fastidiosa symptoms", "Síntomas de Xylella fastidiosa");
    public static readonly SymptomTypes OliveFly = new("OLIVE_FLY", "Olive Fruit Fly damage", "Daño por Mosca del Olivo");
    public static readonly SymptomTypes WaterStress = new("WATER_STRESS", "Signs of severe water stress", "Signos de estrés hídrico severo");
    public static readonly SymptomTypes ChillDeficit = new("CHILL_DEFICIT", "Irregular flowering due to chill deficit", "Floración irregular por déficit de frío");
    public static readonly SymptomTypes ClimateExtreme = new("CLIMATE_EXTREME", "Frost or extreme heat damage", "Daño por helada o calor extremo");
    public static readonly SymptomTypes PeacockSpot = new("PEACOCK_SPOT", "Peacock spot fungus symptoms", "Síntomas del hongo repilo");
    public static readonly SymptomTypes OliveMoth = new("OLIVE_MOTH", "Olive moth larvae damage", "Daño por larvas de polilla del olivo");

    public static IEnumerable<SymptomTypes> GetAll() =>
    [
        Xylella, OliveFly, WaterStress, ChillDeficit, ClimateExtreme, PeacockSpot, OliveMoth
    ];
}
