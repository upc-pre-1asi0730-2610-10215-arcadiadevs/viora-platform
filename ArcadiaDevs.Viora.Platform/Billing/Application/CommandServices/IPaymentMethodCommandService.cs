using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles commands related to <see cref="PaymentMethod" />
///     (REQ-PM-2). Returns <see cref="Result{TValue, TError}" /> (REQ-CC-3)
///     even though this aggregate has no self-guard — kept consistent with
///     every other Billing command service so a future controller/caller can
///     route failures uniformly via <c>BillingActionResultAssembler</c>.
/// </summary>
public interface IPaymentMethodCommandService
{
    /// <summary>
    ///     Upserts a user's payment method display metadata (REQ-PM-2).
    ///     Internal-only — invoked from WU6's webhook reconciliation, not
    ///     wired to any controller in this slice.
    /// </summary>
    Task<Result<PaymentMethod, Error>> Handle(UpsertPaymentMethodCommand command, CancellationToken cancellationToken = default);
}