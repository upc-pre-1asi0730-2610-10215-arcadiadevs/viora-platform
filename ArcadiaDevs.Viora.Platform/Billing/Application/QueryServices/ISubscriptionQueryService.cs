using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Billing.Application.QueryServices;

/// <summary>
///     Service that handles Subscription read queries (REQ-SUB-4). Returns
///     <see cref="Result{TValue, TError}" /> (REQ-CC-3) so
///     <c>SubscriptionsController</c> can map failures via
///     <c>BillingActionResultAssembler</c>.
/// </summary>
public interface ISubscriptionQueryService
{
    /// <summary>
    ///     Retrieves a user's subscription. Fails with
    ///     <c>BillingErrors.NotFound</c> either when the user is unknown to
    ///     IAM (REQ-CC-2) or when the user has no subscription (REQ-SUB-4) —
    ///     two distinct internal guard clauses, same resulting 404.
    /// </summary>
    Task<Result<Subscription, Error>> Handle(GetSubscriptionByUserIdQuery query, CancellationToken cancellationToken = default);
}