using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;

/**
 * <summary>
 *     The session query service interface
 * </summary>
 */
public interface ISessionQueryService
{
    /**
     * <summary>
     *     Handle get-user-sessions query
     * </summary>
     * <param name="query">The get-user-sessions query</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>Sessions ordered current-first, then LastActiveAt desc</returns>
     */
    Task<IReadOnlyList<UserSession>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken);
}
