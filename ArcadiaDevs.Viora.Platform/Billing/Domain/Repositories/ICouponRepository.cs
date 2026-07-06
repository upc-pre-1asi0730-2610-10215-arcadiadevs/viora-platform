using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="Coupon" /> aggregates.
///     Composite unique index on <c>(user_id, code)</c> — per-user
///     idempotency, not a global one-time-use guard (REQ-COUP-2).
/// </summary>
public interface ICouponRepository : IBaseRepository<Coupon>
{
    /// <summary>
    ///     Checks whether the given user has already redeemed the given code
    ///     (REQ-COUP-2 idempotency guard).
    /// </summary>
    Task<bool> ExistsByUserIdAndCodeAsync(int userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists every coupon redeemed by the given user (REQ-COUP-4).
    /// </summary>
    Task<IEnumerable<Coupon>> ListByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}