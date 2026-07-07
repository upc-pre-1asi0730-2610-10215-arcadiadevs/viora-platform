using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="CreateCheckoutCommand" /> (REQ-GATE-3).
/// </summary>
public class CheckoutCommandService(
    IPaymentGateway paymentGateway,
    IPlanRepository planRepository,
    IWebhookReconciliationCommandService webhookReconciliationCommandService)
    : ICheckoutCommandService
{
    private const string ApprovedStatus = "approved";

    public async Task<Result<CheckoutSession, Error>> Handle(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await planRepository.FindByCodeAsync(command.PlanCode, cancellationToken);
            if (plan is null)
            {
                return new Result<CheckoutSession, Error>.Failure(BillingErrors.NotFound);
            }

            // ExternalReference threads {userId}:{planCode}:{interval} through
            // to WU6's webhook reconciliation (design's Webhook flow section).
            var externalReference = $"{command.UserId}:{command.PlanCode}:{command.Interval}";

            if (!paymentGateway.IsConfigured)
            {
                return await CreateAutoApprovedSessionAsync(plan, externalReference, cancellationToken);
            }

            var request = new CheckoutRequest(
                command.UserId,
                command.PlanCode,
                command.Interval,
                plan.PriceAmount,
                plan.Currency,
                externalReference);

            return await paymentGateway.CreateCheckoutAsync(request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new Result<CheckoutSession, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (Exception)
        {
            return new Result<CheckoutSession, Error>.Failure(BillingErrors.InternalServerError);
        }
    }

    /// <summary>
    ///     No sandbox token configured (REQ-GATE-2): instead of failing with
    ///     503, reconciles the payment immediately as approved through the
    ///     same path a real MercadoPago webhook delivery would take
    ///     (<see cref="IWebhookReconciliationCommandService" />), so the
    ///     caller's Subscription/Invoice effects land synchronously. Mirrors
    ///     wa-viora-webapp's mock checkout contract (server/index.js) exactly
    ///     — <c>preferenceId: "mock-{epoch-ms}"</c>, <c>checkoutUrl: "/dashboard"</c> —
    ///     since the real MercadoPago integration was dropped in favor of
    ///     instant approval on both sides (decided 2026-07-06).
    /// </summary>
    private async Task<Result<CheckoutSession, Error>> CreateAutoApprovedSessionAsync(
        Plan plan,
        string externalReference,
        CancellationToken cancellationToken)
    {
        var reconcileCommand = new ReconcilePaymentCommand(
            Id: null,
            Type: null,
            PaymentId: $"fake-{Guid.NewGuid():N}",
            ExternalReference: externalReference,
            Status: ApprovedStatus,
            Amount: plan.PriceAmount,
            Currency: plan.Currency,
            CardBrand: null,
            CardLast4: null,
            CardExpMonth: null,
            CardExpYear: null);

        var reconcileResult = await webhookReconciliationCommandService.Handle(reconcileCommand, cancellationToken);
        if (reconcileResult is Result<Unit, Error>.Failure reconcileFailure)
        {
            return new Result<CheckoutSession, Error>.Failure(reconcileFailure.Error);
        }

        return new Result<CheckoutSession, Error>.Success(
            new CheckoutSession(
                CheckoutUrl: "/dashboard",
                PreferenceId: $"mock-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                ExternalReference: externalReference));
    }
}
