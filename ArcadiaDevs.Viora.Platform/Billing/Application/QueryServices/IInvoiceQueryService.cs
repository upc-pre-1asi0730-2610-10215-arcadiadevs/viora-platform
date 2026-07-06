using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;

/// <summary>
///     Service that handles Invoice read queries (REQ-INV-3). Returns
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>InvoicesController</c> can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface IInvoiceQueryService
{
    /// <summary>
    ///     Lists every invoice belonging to a user. <c>userId</c> is direct
    ///     client input on this read endpoint and MUST be IAM-validated
    ///     (REQ-CC-2) — unlike <c>PaymentMethodQueryService</c>'s spec-exempt
    ///     list read.
    /// </summary>
    Task<Result<IEnumerable<Invoice>, Error>> Handle(GetInvoicesByUserIdQuery query, CancellationToken cancellationToken = default);
}