using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="PlanResource" /> from the <see cref="Plan" /> aggregate.
/// </summary>
public static class PlanResourceFromEntityAssembler
{
    public static PlanResource ToResourceFromEntity(Plan plan)
    {
        return new PlanResource(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.PriceAmount,
            plan.Currency,
            plan.Interval.ToString(),
            plan.Tagline,
            plan.Features,
            plan.PlotLimit,
            plan.IotLimit);
    }
}