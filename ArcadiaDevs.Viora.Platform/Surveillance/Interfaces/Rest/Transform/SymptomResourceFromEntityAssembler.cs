using ArcadiaDevs.Viora.Platform.Surveillance.Domain.Model.Entities;
using ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Surveillance.Interfaces.Rest.Transform;

/// <summary>
/// Assembler to map a <see cref="SymptomDictionaryItem"/> entity to a <see cref="SymptomResource"/>.
/// </summary>
public static class SymptomResourceFromEntityAssembler
{
    /// <summary>
    /// Transforms the entity into a resource, selecting the correct language for the description.
    /// </summary>
    /// <param name="entity">The symptom dictionary item.</param>
    /// <param name="language">The requested language code (e.g., "en", "es").</param>
    /// <returns>The mapped symptom resource.</returns>
    public static SymptomResource ToResourceFromEntity(SymptomDictionaryItem entity, string language)
    {
        string description = language.StartsWith("es", StringComparison.OrdinalIgnoreCase)
            ? entity.DescriptionEs
            : entity.DescriptionEn;

        return new SymptomResource(entity.Id, description);
    }
}
