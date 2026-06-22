using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;

public class ChillRequirementResolver(ChillRequirementPolicy policy)
{
    public ChillRequirement ResolveFor(Plot? plot)
    {
        if (plot?.ChillRequirementOverride != null)
        {
            return plot.ChillRequirementOverride;
        }

        return policy.ResolveFor(plot?.CropType);
    }

    public ChillRequirement ResolveDefault()
    {
        return policy.ResolveFor(null);
    }
}
