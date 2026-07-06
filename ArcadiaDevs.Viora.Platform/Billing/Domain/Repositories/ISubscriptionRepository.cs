using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="Subscription" /> aggregates.
///     Unique index on <see cref="Subscription.UserId" /> (REQ-SUB-1 — one
///     subscription per user).
/// </summary>
public interface ISubscriptionRepository : IBaseRepository<Subscription>
{
    /// <summary>
    ///     Finds the (at most one) subscription belonging to the given user.
    /// </summary>
    Task<Subscription?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}