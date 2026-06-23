using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembles a chill requirement resource from its domain value object.
/// </summary>
public static class ChillRequirementResourceAssembler
{
    public static ChillRequirementResource ToResource(this ChillRequirement chillRequirement)
    {
        return new ChillRequirementResource(
            chillRequirement.Portions.Value,
            chillRequirement.Source.ToString(),
            chillRequirement.Model.DisplayName(),
            chillRequirement.Model.UnitLabel());
    }
}
