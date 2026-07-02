using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;

/**
 * <summary>
 *     The role query service interface
 * </summary>
 * <remarks>
 *     This service contract specifies handling behavior used to query roles
 * </remarks>
 */
public interface IRoleQueryService
{
    /**
     * <summary>
     *     Handle get all roles query
     * </summary>
     * <param name="query">The get all roles query</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>The list of roles</returns>
     */
    Task<IEnumerable<Role>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken);
}
