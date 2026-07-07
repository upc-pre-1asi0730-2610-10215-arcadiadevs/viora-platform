using ArcadiaDevs.Viora.Platform.Billing.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
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
    IProfileContextFacade profileContextFacade,
    IUnitOfWork unitOfWork)
    : ISubscriptionCommandService
{
    private const string SpecialistPlanPrefix = "specialist-";

    private const string SpecialistProPlanCode = "specialist-pro";

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

    /// <summary>
    ///     Switches (or creates) the user's subscription (REQ-SUB-3). Upserts
    ///     — when no subscription exists yet, one is created directly as
    ///     <c>ACTIVE</c> for the requested plan, matching the pattern
    ///     <c>WebhookReconciliationCommandService.ApplySubscriptionEffectAsync</c>
    ///     already uses for the webhook entry point, so both paths behave the
    ///     same instead of only the webhook one upserting.
    /// </summary>
    /// <remarks>
    ///     After a successful save, syncs the specialist Pro badge
    ///     (<see cref="IProfileContextFacade.SetProBadgeAsync" />) when
    ///     <paramref name="command" />'s <c>PlanCode</c> starts with
    ///     <c>"specialist-"</c> — enabled only for <c>specialist-pro</c>,
    ///     disabled for <c>specialist-plus</c>. Grower plans
    ///     (<c>free</c>/<c>basic</c>/<c>pro</c>/<c>enterprise</c>) never touch
    ///     the badge — the sync call is guarded behind the prefix check, not
    ///     called unconditionally.
    /// </remarks>
    public async Task<Result<Subscription, Error>> Handle(
        SwitchPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await planRepository.FindByCodeAsync(command.PlanCode, cancellationToken);
            if (plan is null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            var subscription = await subscriptionRepository.FindByUserIdAsync(command.UserId, cancellationToken);

            if (subscription is null)
            {
                subscription = new Subscription(
                    command.UserId, command.PlanCode, command.Interval, ComputePeriodEnd(command.Interval));

                var activateResult = subscription.Activate();
                if (activateResult is Result<Unit, Error>.Failure activateFailure)
                {
                    return new Result<Subscription, Error>.Failure(activateFailure.Error);
                }

                await subscriptionRepository.AddAsync(subscription, cancellationToken);
            }
            else
            {
                var switchResult = subscription.SwitchTo(command.PlanCode, command.Interval);
                if (switchResult is Result<Unit, Error>.Failure switchFailure)
                {
                    return new Result<Subscription, Error>.Failure(switchFailure.Error);
                }

                subscriptionRepository.Update(subscription);
            }

            await unitOfWork.CompleteAsync(cancellationToken);

            if (command.PlanCode.StartsWith(SpecialistPlanPrefix, StringComparison.Ordinal))
            {
                var enableProBadge = string.Equals(command.PlanCode, SpecialistProPlanCode, StringComparison.Ordinal);
                await profileContextFacade.SetProBadgeAsync(command.UserId, enableProBadge, cancellationToken);
            }

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

    private static DateTimeOffset ComputePeriodEnd(PlanInterval interval)
    {
        return interval == PlanInterval.ANNUAL
            ? DateTimeOffset.UtcNow.AddYears(1)
            : DateTimeOffset.UtcNow.AddMonths(1);
    }
}
