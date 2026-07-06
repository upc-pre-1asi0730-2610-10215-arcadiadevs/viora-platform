using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;

/// <summary>
///     Service that handles PaymentMethod read queries (REQ-PM-3). Returns
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>PaymentMethodsController</c> can map failures via
///     <c>BillingActionResultAssembler</c>, matching <c>IPlanQueryService</c>'s
///     list-query shape.
/// </summary>
public interface IPaymentMethodQueryService
{
    /// <summary>
    ///     Lists the payment method(s) belonging to a user. Not IAM-validated
    ///     (REQ-PM-3 does not invoke REQ-CC-2, unlike REQ-INV-3's read
    ///     endpoint — this is a locked spec asymmetry, not an oversight; see
    ///     <c>PaymentMethodQueryService</c>'s remarks).
    /// </summary>
    Task<Result<IEnumerable<PaymentMethod>, Error>> Handle(GetPaymentMethodsByUserIdQuery query, CancellationToken cancellationToken = default);
}