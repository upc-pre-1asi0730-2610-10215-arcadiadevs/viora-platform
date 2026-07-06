using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.CommandServices;

/// <summary>
///     Handles <see cref="Subscription" /> commands (REQ-SUB-1..3). FK
///     validation follows the same direct-injection pattern as
///     <c>InterventionRequestCommandService</c> — no wrapper adapter around
///     <see cref="IIamContextFacade" /> (design's Cross-BC Integration
///     section).
/// </summary>
public class SubscriptionCommandService(
    ISubscriptionRepository subscriptionRepository,
    IPlanRepository planRepository,
    IIamContextFacade iamContextFacade,
    IUnitOfWork unitOfWork)
    : ISubscriptionCommandService
{
    public async Task<Result<Subscription, Error>> Handle(
        CreateSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await iamContextFacade.ExistsUserAsync(command.UserId, cancellationToken))
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            var plan = await planRepository.FindByCodeAsync(command.PlanCode, cancellationToken);
            if (plan is null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            // REQ-SUB-1: one subscription per user — guard before insert so a
            // duplicate attempt maps to a clean 409 instead of an unhandled
            // unique-index DbUpdateException (mirrors WU1's ExistsByCodeAsync
            // idempotency-guard idiom).
            var existing = await subscriptionRepository.FindByUserIdAsync(command.UserId, cancellationToken);
            if (existing is not null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.ConflictError);
            }

            var subscription = new Subscription(
                command.UserId,
                command.PlanCode,
                command.Interval,
                command.CurrentPeriodEnd);

            await subscriptionRepository.AddAsync(subscription, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Subscription, Error>.Success(subscription);
        }
        catch (ArgumentException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.ValidationError);
        }
        catch (OperationCanceledException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.InternalServerError);
        }
    }

    public async Task<Result<Subscription, Error>> Handle(
        CancelSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await subscriptionRepository.FindByUserIdAsync(command.UserId, cancellationToken);
            if (subscription is null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            var cancelResult = subscription.Cancel();
            if (cancelResult is Result<Unit, Error>.Failure cancelFailure)
            {
                return new Result<Subscription, Error>.Failure(cancelFailure.Error);
            }

            subscriptionRepository.Update(subscription);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Subscription, Error>.Success(subscription);
        }
        catch (OperationCanceledException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.InternalServerError);
        }
    }

    public async Task<Result<Subscription, Error>> Handle(
        SwitchPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var subscription = await subscriptionRepository.FindByUserIdAsync(command.UserId, cancellationToken);
            if (subscription is null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            var plan = await planRepository.FindByCodeAsync(command.PlanCode, cancellationToken);
            if (plan is null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            var switchResult = subscription.SwitchTo(command.PlanCode, command.Interval);
            if (switchResult is Result<Unit, Error>.Failure switchFailure)
            {
                return new Result<Subscription, Error>.Failure(switchFailure.Error);
            }

            subscriptionRepository.Update(subscription);
            await unitOfWork.CompleteAsync(cancellationToken);

            return new Result<Subscription, Error>.Success(subscription);
        }
        catch (OperationCanceledException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.OperationCancelled);
        }
        catch (DbUpdateException)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.DatabaseError);
        }
        catch (Exception)
        {
            return new Result<Subscription, Error>.Failure(BillingErrors.InternalServerError);
        }
    }
}
