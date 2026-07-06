using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;

/// <summary>
///     The user-session repository.
/// </summary>
public interface IUserSessionRepository : IBaseRepository<UserSession>
{
    /// <summary>
    ///     Finds all sessions belonging to the given user.
    /// </summary>
    Task<IReadOnlyList<UserSession>> FindByUserIdAsync(int userId, CancellationToken cancellationToken);
}
