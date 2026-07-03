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
    Task<Specialist?> FindByProfileUserIdAsync(int profileUserId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks whether a specialist already exists for the given Profile
    ///     UserId — used by the idempotent startup seed.
    /// </summary>
    Task<bool> ExistsByProfileUserIdAsync(int profileUserId, CancellationToken cancellationToken = default);
}
