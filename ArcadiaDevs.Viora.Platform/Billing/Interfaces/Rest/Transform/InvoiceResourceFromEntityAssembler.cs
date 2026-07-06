using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Resources;

namespace ArcadiaDevs.Viora.Platform.Billing.Interfaces.Rest.Transform;

/// <summary>
///     Assembles <see cref="InvoiceResource" /> from the <see cref="Invoice" />
///     aggregate.
/// </summary>
public static class InvoiceResourceFromEntityAssembler
{
    public static InvoiceResource ToResourceFromEntity(Invoice invoice)
    {
        return new InvoiceResource(
            invoice.Id,
            invoice.UserId,
            invoice.IssuedAt,
            invoice.Description,
            invoice.Amount,
            invoice.Currency,
            invoice.Status.ToString(),
            invoice.ExternalPaymentId);
    }
}