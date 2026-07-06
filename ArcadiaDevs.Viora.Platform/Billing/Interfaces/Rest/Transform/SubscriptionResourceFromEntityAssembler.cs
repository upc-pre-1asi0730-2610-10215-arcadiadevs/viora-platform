using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="SubscriptionResource" /> from the
///     <see cref="Subscription" /> aggregate.
/// </summary>
public static class SubscriptionResourceFromEntityAssembler
{
    public static SubscriptionResource ToResourceFromEntity(Subscription subscription)
    {
        return new SubscriptionResource(
            subscription.Id,
            subscription.UserId,
            subscription.PlanCode,
            subscription.Interval.ToString(),
            subscription.Status.ToString(),
            subscription.CurrentPeriodEnd);
    }
}