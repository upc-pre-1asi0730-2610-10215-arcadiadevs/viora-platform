using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Transform;

/// <summary>
///     Assembles a configure chill requirement command from its REST resource.
/// </summary>
public static class ConfigureChillRequirementCommandFromResourceAssembler
{
    public static ConfigureChillRequirementCommand ToCommand(this ConfigureChillRequirementResource resource, int plotId, int userId)
    {
        return new ConfigureChillRequirementCommand(
            plotId,
            userId,
            resource.ChillRequirementPortions);
    }
}
