using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="ReconcilePaymentCommand" /> (REQ-GATE-4, REQ-GATE-5)
///     — the highest-risk flow in the Billing port. Resolves payment details,
///     checks webhook-replay idempotency, then applies the Invoice/
///     Subscription/PaymentMethod side effects in that order (spec's
///     "Design-deferred items" #2: internal ordering beyond "fetch before
///     persist" is unspecified — this order matches the tasks doc's literal
///     6.3/6.4 sequencing).
/// </summary>
/// <remarks>
///     Every failure path returns a mapped <see cref="Result{TValue, TError}" />
///     (REQ-CC-3) rather than throwing — the caller
///     (<c>MercadoPagoWebhookController</c>) logs failures and always
///     answers 200 regardless (REQ-GATE-4), so this service never needs to
///     distinguish "the caller should see this error" from "log and move
///     on": every error is the latter.
/// </remarks>
public class WebhookReconciliationCommandService(
    IPaymentGateway paymentGateway,
    IInvoiceRepository invoiceRepository,
    IInvoiceCommandService invoiceCommandService,
    ISubscriptionRepository subscriptionRepository,
    ISubscriptionCommandService subscriptionCommandService,
    IPaymentMethodCommandService paymentMethodCommandService,
    IUnitOfWork unitOfWork)
    : IWebhookReconciliationCommandService
{
    private const string ApprovedStatus = "approved";

    public async Task<Result<Unit, Error>> Handle(
        ReconcilePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var paymentInfoResult = await ResolvePaymentInfoAsync(command, cancellationToken);
            if (paymentInfoResult is Result<PaymentInfo, Error>.Failure paymentInfoFailure)
            {
                return new Result<Unit, Error>.Failure(paymentInfoFailure.Error);
            }

            var paymentInfo = ((Result<PaymentInfo, Error>.Success)paymentInfoResult).Value;

            // REQ-INV-1: webhook-replay idempotency — a previously-reconciled
            // payment (same external payment id) is a replay-safe no-op.
            var existingInvoice = await invoiceRepository.FindByExternalPaymentIdAsync(paymentInfo.PaymentId, cancellationToken);
            if (existingInvoice is not null)
            {
                return new Result<Unit, Error>.Success(Unit.Value);
            }

            if (!TryParseExternalReference(paymentInfo.ExternalReference, out var userId, out var planCode, out var interval))
            {
                return new Result<Unit, Error>.Failure(BillingErrors.ValidationError);
            }

            var isApproved = string.Equals(paymentInfo.Status, ApprovedStatus, StringComparison.OrdinalIgnoreCase);

            var invoiceCommand = new CreateInvoiceCommand(
                userId,
                DateTimeOffset.UtcNow,
                $"Subscription payment for plan {planCode}",
                paymentInfo.Amount,
                isApproved ? paymentInfo.PaymentId : null,
                paymentInfo.Currency);

            var invoiceResult = await invoiceCommandService.Handle(invoiceCommand, cancellationToken);
            if (invoiceResult is Result<Invoice, Error>.Failure invoiceFailure)
            {
                return new Result<Unit, Error>.Failure(invoiceFailure.Error);
            }

            if (!isApproved)
            {
                // REQ-GATE-5 (non-approved path): Invoice recorded as FAILED,
                // Subscription untouched.
                return new Result<Unit, Error>.Success(Unit.Value);
            }

            var subscriptionEffectResult = await ApplySubscriptionEffectAsync(userId, planCode, interval, cancellationToken);
            if (subscriptionEffectResult is Result<Unit, Error>.Failure subscriptionFailure)
            {
                return new Result<Unit, Error>.Failure(subscriptionFailure.Error);
            }

            if (HasCardData(paymentInfo))
            {
                var upsertResult = await paymentMethodCommandService.Handle(
                    new UpsertPaymentMethodCommand(
                        userId,
                        paymentInfo.CardBrand!,
                        paymentInfo.CardLast4!,
                        paymentInfo.CardExpMonth!.Value,
                        paymentInfo.CardExpYear!.Value,
                        IsDefault: true),
                    cancellationToken);

                if (upsertResult is Result<PaymentMethod, Error>.Failure upsertFailure)
                {
                    return new Result<Unit, Error>.Failure(upsertFailure.Error);
                }
            }

            return new Result<Unit, Error>.Success(Unit.Value);
        }
        catch (OperationCanceledException)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<Unit, Error>.Failure(BillingErrors.InternalServerError);
        }
    }

    /// <summary>
    ///     Resolves <see cref="PaymentInfo" /> via <see cref="IPaymentGateway.FetchPaymentAsync" />
    ///     when configured, or via the inline synthetic payload when not
    ///     (design's demoability mechanism). 400s if the inline fields are
    ///     missing while unconfigured.
    /// </summary>
    private async Task<Result<PaymentInfo, Error>> ResolvePaymentInfoAsync(
        ReconcilePaymentCommand command,
        CancellationToken cancellationToken)
    {
        if (paymentGateway.IsConfigured)
        {
            if (string.IsNullOrWhiteSpace(command.Id))
            {
                return new Result<PaymentInfo, Error>.Failure(BillingErrors.ValidationError);
            }

            return await paymentGateway.FetchPaymentAsync(command.Id, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(command.PaymentId)
            || string.IsNullOrWhiteSpace(command.ExternalReference)
            || string.IsNullOrWhiteSpace(command.Status)
            || command.Amount is null
            || string.IsNullOrWhiteSpace(command.Currency))
        {
            return new Result<PaymentInfo, Error>.Failure(BillingErrors.ValidationError);
        }

        var inlinePaymentInfo = new PaymentInfo(
            command.PaymentId,
            command.Status,
            command.Amount.Value,
            command.Currency,
            command.ExternalReference,
            command.CardBrand,
            command.CardLast4,
            command.CardExpMonth,
            command.CardExpYear);

        return new Result<PaymentInfo, Error>.Success(inlinePaymentInfo);
    }

    /// <summary>
    ///     Parses <c>"{userId}:{planCode}:{interval}"</c> — the
    ///     <see cref="CheckoutRequest" />-time format set by WU5's
    ///     <c>CheckoutCommandService</c>.
    /// </summary>
    private static bool TryParseExternalReference(
        string externalReference,
        out int userId,
        out string planCode,
        out PlanInterval interval)
    {
        userId = 0;
        planCode = string.Empty;
        interval = default;

        var parts = externalReference.Split(':');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out userId) || userId <= 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(parts[1]))
        {
            return false;
        }

        if (!Enum.TryParse(parts[2], ignoreCase: true, out interval))
        {
            return false;
        }

        planCode = parts[1];
        return true;
    }

    /// <summary>
    ///     Applies the Subscription side effect for an approved payment
    ///     (REQ-SUB-3, REQ-GATE-5): <c>Activate()</c> when the subscription
    ///     doesn't exist yet or is still <c>PENDING</c> (first payment);
    ///     otherwise <c>Renew()</c> (same plan) or <c>SwitchTo()</c>+
    ///     <c>Renew()</c> (plan change) on the existing <c>ACTIVE</c>
    ///     subscription. Mutates directly on ONE loaded aggregate instance
    ///     (never through <c>ISubscriptionCommandService.Handle(SwitchPlanCommand)</c>
    ///     for an already-loaded subscription — that method does its own
    ///     independent load+save cycle and would double-track the same
    ///     entity in this request's DbContext). <c>planCode</c> is treated
    ///     as pre-validated: <c>CheckoutCommandService</c> already confirmed
    ///     it exists in the Plan catalog at checkout time, before this
    ///     <c>ExternalReference</c> was ever generated.
    /// </summary>
    private async Task<Result<Unit, Error>> ApplySubscriptionEffectAsync(
        int userId,
        string planCode,
        PlanInterval interval,
        CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FindByUserIdAsync(userId, cancellationToken);
        var periodEnd = ComputePeriodEnd(interval);

        if (subscription is null)
        {
            var createResult = await subscriptionCommandService.Handle(
                new CreateSubscriptionCommand(userId, planCode, interval, periodEnd),
                cancellationToken);

            if (createResult is Result<Subscription, Error>.Failure createFailure)
            {
                return new Result<Unit, Error>.Failure(createFailure.Error);
            }

            subscription = ((Result<Subscription, Error>.Success)createResult).Value;
            var activateResult = subscription.Activate();
            if (activateResult is Result<Unit, Error>.Failure activateFailure)
            {
                return new Result<Unit, Error>.Failure(activateFailure.Error);
            }

            subscriptionRepository.Update(subscription);
            await unitOfWork.CompleteAsync(cancellationToken);
            return new Result<Unit, Error>.Success(Unit.Value);
        }

        Result<Unit, Error> transitionResult;

        if (subscription.Status == SubscriptionStatus.PENDING)
        {
            transitionResult = subscription.Activate();
        }
        else if (string.Equals(subscription.PlanCode, planCode, StringComparison.Ordinal)
                 && subscription.Interval == interval)
        {
            transitionResult = subscription.Renew(periodEnd);
        }
        else
        {
            transitionResult = subscription.SwitchTo(planCode, interval);
            if (transitionResult is Result<Unit, Error>.Success)
            {
                // Extend the period alongside the switch — SwitchTo doesn't
                // change Status, so the identical ACTIVE-only guard on Renew
                // cannot fail here.
                subscription.Renew(periodEnd);
            }
        }

        if (transitionResult is Result<Unit, Error>.Failure transitionFailure)
        {
            return new Result<Unit, Error>.Failure(transitionFailure.Error);
        }

        subscriptionRepository.Update(subscription);
        await unitOfWork.CompleteAsync(cancellationToken);
        return new Result<Unit, Error>.Success(Unit.Value);
    }

    private static DateTimeOffset ComputePeriodEnd(PlanInterval interval)
    {
        return interval == PlanInterval.ANNUAL
            ? DateTimeOffset.UtcNow.AddYears(1)
            : DateTimeOffset.UtcNow.AddMonths(1);
    }

    private static bool HasCardData(PaymentInfo paymentInfo)
    {
        return !string.IsNullOrWhiteSpace(paymentInfo.CardBrand)
               && !string.IsNullOrWhiteSpace(paymentInfo.CardLast4)
               && paymentInfo.CardExpMonth is not null
               && paymentInfo.CardExpYear is not null;
    }
}
