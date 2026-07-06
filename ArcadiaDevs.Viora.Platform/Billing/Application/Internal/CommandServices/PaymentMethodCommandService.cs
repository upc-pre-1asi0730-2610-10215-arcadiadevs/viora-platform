using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="PaymentMethod" /> commands (REQ-PM-2). No
///     <c>IIamContextFacade</c> dependency — <c>UserId</c> is internally
///     derived from an already-validated Subscription/checkout flow, exempt
///     from REQ-CC-2 per that REQ's own exemption clause.
/// </summary>
public class PaymentMethodCommandService(
    IPaymentMethodRepository paymentMethodRepository,
    IUnitOfWork unitOfWork)
    : IPaymentMethodCommandService
{
    public async Task<Result<PaymentMethod, Error>> Handle(
        UpsertPaymentMethodCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await paymentMethodRepository.FindByUserIdAsync(command.UserId, cancellationToken);

            if (existing is null)
            {
                var paymentMethod = new PaymentMethod(
                    command.UserId,
                    command.Brand,
                    command.Last4,
                    command.ExpMonth,
                    command.ExpYear,
                    command.IsDefault);

                await paymentMethodRepository.AddAsync(paymentMethod, cancellationToken);
                await unitOfWork.CompleteAsync(cancellationToken);

                return new Result<PaymentMethod, Error>.Success(paymentMethod);
            }

            // REQ-PM-2: reuse the existing single row per user — ctor-replace
            // the display metadata on the already-tracked instance, then
            // repo Update persists the change without inserting a new row.
            existing.ReplaceCardMetadata(
                command.Brand,
                command.Last4,
                command.ExpMonth,
                command.ExpYear,
                command.IsDefault);

            paymentMethodRepository.Update(existing);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<PaymentMethod, Error>.Success(existing);
        }
        catch (ArgumentException)
        {
            return new Result<PaymentMethod, Error>.Failure(BillingErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<PaymentMethod, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<PaymentMethod, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<PaymentMethod, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
