using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="PaymentMethod" /> aggregates.
///     Unique index on <see cref="PaymentMethod.UserId" /> (REQ-PM-2 — a
///     single reused row per user).
/// </summary>
public interface IPaymentMethodRepository : IBaseRepository<PaymentMethod>
{
    /// <summary>
    ///     Finds the (at most one) payment method belonging to the given
    ///     user — used both for the upsert path (REQ-PM-2) and the list
    ///     read path (REQ-PM-3).
    /// </summary>
    Task<PaymentMethod?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}