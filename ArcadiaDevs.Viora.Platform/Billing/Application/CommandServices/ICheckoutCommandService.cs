using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that handles <see cref="CreateCheckoutCommand" /> (REQ-GATE-3).
///     Returns <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>CheckoutsController</c> can route failures uniformly via
///     <c>BillingActionResultAssembler</c>, including the
///     <c>PaymentGatewayNotConfigured</c> → 503 mapping.
/// </summary>
public interface ICheckoutCommandService
{
    /// <summary>
    ///     Creates a checkout session for a user and target plan
    ///     (REQ-GATE-3). Fails gracefully (mapped 503, not an unhandled
    ///     exception) when the payment gateway is unconfigured.
    /// </summary>
    Task<Result<CheckoutSession, Error>> Handle(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken = default);
}