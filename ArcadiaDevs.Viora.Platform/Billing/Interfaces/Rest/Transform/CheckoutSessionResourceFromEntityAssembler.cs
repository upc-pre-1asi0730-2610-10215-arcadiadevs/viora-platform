using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="CheckoutSessionResource" /> from a
///     <see cref="CheckoutSession" /> read-model record.
/// </summary>
public static class CheckoutSessionResourceFromEntityAssembler
{
    public static CheckoutSessionResource ToResourceFromEntity(CheckoutSession session)
    {
        return new CheckoutSessionResource(session.CheckoutUrl, session.ExternalReference);
    }
}