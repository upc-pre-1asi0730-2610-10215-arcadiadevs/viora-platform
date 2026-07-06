using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="Invoice" /> aggregates.
///     Unique index on <see cref="Invoice.ExternalPaymentId" />, nullable-safe
///     (REQ-INV-1 — duplicate webhook deliveries cannot double-invoice).
/// </summary>
public interface IInvoiceRepository : IBaseRepository<Invoice>
{
    /// <summary>
    ///     Finds an invoice by its (unique, nullable) external payment
    ///     identifier — used both for the webhook-replay idempotency check
    ///     (REQ-INV-1) and the pre-insert duplicate guard in
    ///     <c>InvoiceCommandService</c>.
    /// </summary>
    Task<Invoice?> FindByExternalPaymentIdAsync(string externalPaymentId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists every invoice belonging to the given user (REQ-INV-3).
    /// </summary>
    Task<IEnumerable<Invoice>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}