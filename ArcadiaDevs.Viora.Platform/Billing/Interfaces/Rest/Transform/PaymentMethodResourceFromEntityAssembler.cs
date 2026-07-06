using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="PaymentMethodResource" /> from the
///     <see cref="PaymentMethod" /> aggregate.
/// </summary>
public static class PaymentMethodResourceFromEntityAssembler
{
    public static PaymentMethodResource ToResourceFromEntity(PaymentMethod paymentMethod)
    {
        return new PaymentMethodResource(
            paymentMethod.Id,
            paymentMethod.UserId,
            paymentMethod.Brand,
            paymentMethod.Last4,
            paymentMethod.ExpMonth,
            paymentMethod.ExpYear,
            paymentMethod.IsDefault);
    }
}