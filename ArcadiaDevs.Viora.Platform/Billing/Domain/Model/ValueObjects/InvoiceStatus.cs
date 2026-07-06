namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;

/// <summary>
///     Lifecycle status of an <see cref="Aggregates.Invoice" /> (REQ-INV-4).
/// </summary>
public enum InvoiceStatus
{
    PAID,
    PENDING,
    FAILED
}