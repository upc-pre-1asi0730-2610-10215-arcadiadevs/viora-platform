using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Application.Internal.OutboundServices;
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
    IPlanRepository planRepository)
    : ICheckoutCommandService
{
    public async Task<Result<CheckoutSession, Error>> Handle(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // REQ-GATE-3: fail gracefully (mapped 503), never an unhandled
            // exception — short-circuits before any Plan lookup or outbound
            // HTTP call is attempted when the gateway has no sandbox token.
            if (!paymentGateway.IsConfigured)
            {
                return new Result<CheckoutSession, Error>.Failure(BillingErrors.PaymentGatewayNotConfigured);
            }

            var plan = await planRepository.FindByCodeAsync(command.PlanCode, cancellationToken);
            if (plan is null)
            {
                return new Result<CheckoutSession, Error>.Failure(BillingErrors.NotFound);
            }

            // ExternalReference threads {userId}:{planCode}:{interval} through
            // to WU6's webhook reconciliation (design's Webhook flow section).
            var externalReference = $"{command.UserId}:{command.PlanCode}:{command.Interval}";

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
}
