using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="Invoice" />
///     (REQ-INV-1, REQ-INV-2). Returns <see cref="Result{TValue, TError}" />
///     (REQ-CC-3) so a future caller (WU6's webhook reconciliation) can
///     route failures uniformly via <c>BillingActionResultAssembler</c>-style
///     mapping.
/// </summary>
public interface IInvoiceCommandService
{
    /// <summary>
    ///     Creates a new invoice for a user (REQ-INV-1, REQ-INV-2).
    ///     Internal-only — invoked from WU6's webhook reconciliation, not
    ///     wired to any controller in this slice.
    /// </summary>
    Task<Result<Invoice, Error>> Handle(CreateInvoiceCommand command, CancellationToken cancellationToken = default);
}