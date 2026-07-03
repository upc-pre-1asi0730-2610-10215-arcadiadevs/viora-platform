using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;

/// <summary>
///     Repository for the Profile aggregate.
/// </summary>
public interface IProfileRepository : IBaseRepository<ProfileAggregate>
{
    /// <summary>
    ///     Find a profile by the associated user id.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The profile if found, otherwise null.</returns>
    Task<ProfileAggregate?> FindByUserIdAsync(int userId, CancellationToken ct = default);
}
