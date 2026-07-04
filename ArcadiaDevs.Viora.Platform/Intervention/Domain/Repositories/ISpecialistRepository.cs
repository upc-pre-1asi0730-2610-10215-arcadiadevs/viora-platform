using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Intervention.Domain.Repositories;

/// <summary>
///     Repository interface for managing <see cref="Specialist" /> aggregates.
/// </summary>
public interface ISpecialistRepository : IBaseRepository<Specialist>
{
    /// <summary>
    ///     Finds a specialist by its backing Profile's UserId.
    /// </summary>
    /// <remarks>
    ///     Not called anywhere in WU1 — kept as a forward-looking lookup
    ///     (e.g. for a future "is this authenticated user a specialist"
    ///     check) rather than a currently-exercised code path.
    /// </remarks>
    Task<Specialist?> FindByProfileUserIdAsync(int profileUserId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a specialist already exists for the given Profile
    ///     UserId — used by the idempotent startup seed.
    /// </summary>
    Task<bool> ExistsByProfileUserIdAsync(int profileUserId, CancellationToken cancellationToken = default);
}
