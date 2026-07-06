using ArcadiaDevs.Viora.Platform.Billing.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Billing.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="Plan" /> aggregates.
///     Unique index on <see cref="Plan.Code" /> (REQ-PLAN-1).
/// </summary>
public interface IPlanRepository : IBaseRepository<Plan>
{
    /// <summary>
    ///     Finds a plan by its unique catalog code.
    /// </summary>
    Task<Plan?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a plan already exists for the given code — used by
    ///     the idempotent startup seed (REQ-PLAN-1).
    /// </summary>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
}