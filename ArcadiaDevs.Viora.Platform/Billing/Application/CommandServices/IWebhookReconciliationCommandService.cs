using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;

/// <summary>
///     Service that reconciles inbound MercadoPago payment notifications
///     (REQ-GATE-4, REQ-GATE-5). Returns <see cref="Result{TValue, TError}" />
///     (REQ-CC-3) even though the controller always answers 200 regardless
///     of the outcome — the caller (controller) logs failures rather than
///     surfacing them, matching REQ-GATE-4's "errors logged, not surfaced"
///     accepted gap.
/// </summary>
public interface IWebhookReconciliationCommandService
{
    /// <summary>
    ///     Resolves payment details, checks webhook-replay idempotency, and
    ///     applies the Invoice/Subscription/PaymentMethod side effects
    ///     (REQ-GATE-5). Internal-only — invoked exclusively from
    ///     <c>MercadoPagoWebhookController</c>.
    /// </summary>
    Task<Result<Unit, Error>> Handle(ReconcilePaymentCommand command, CancellationToken cancellationToken = default);
}