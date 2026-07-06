using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.Internal.QueryServices;

/**
 * <summary>
 *     Handles user-session queries.
 * </summary>
 */
public class SessionQueryService(IUserSessionRepository userSessionRepository) : ISessionQueryService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSession>> Handle(
        GetUserSessionsQuery query,
        CancellationToken cancellationToken)
    {
        var sessions = await userSessionRepository.FindByUserIdAsync(query.UserId, cancellationToken);

        // Current-first, then LastActiveAt desc (mirrors OS's SessionQueryServiceImpl comparator).
        return sessions
            .OrderByDescending(s => s.IsCurrent)
            .ThenByDescending(s => s.LastActiveAt)
            .ToList();
    }
}
