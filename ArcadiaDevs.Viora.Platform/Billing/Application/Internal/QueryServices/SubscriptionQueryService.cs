using ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.Internal.QueryServices;

/// <summary>
///     Handles Subscription read queries (REQ-SUB-4), mapping failures
///     through <see cref="Result{TValue, TError}" /> (REQ-CC-3).
/// </summary>
public class SubscriptionQueryService(
    ISubscriptionRepository subscriptionRepository,
    IIamContextFacade iamContextFacade)
    : ISubscriptionQueryService
{
    public async Task<Result<Subscription, Error>> Handle(
        GetSubscriptionByUserIdQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // REQ-CC-2: unknown userId (IAM lookup fails) -> 404, checked
            // BEFORE the subscription lookup so this case stays distinct from
            // REQ-SUB-4's "known user, no subscription" 404 below.
            if (!await iamContextFacade.ExistsUserAsync(query.UserId, cancellationToken))
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

            var subscription = await subscriptionRepository.FindByUserIdAsync(query.UserId, cancellationToken);
            if (subscription is null)
            {
                return new Result<Subscription, Error>.Failure(BillingErrors.NotFound);
            }

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
