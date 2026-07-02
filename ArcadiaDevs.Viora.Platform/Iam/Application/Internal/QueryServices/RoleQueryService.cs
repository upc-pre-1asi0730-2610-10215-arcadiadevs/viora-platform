using ArcadiaDevs.Viora.Platform.Iam.Application.QueryServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Queries;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.Internal.QueryServices;

/**
 * <summary>
 *     The role query service implementation class
 * </summary>
 * <remarks>
 *     This class is used to handle role queries
 * </remarks>
 */
public class RoleQueryService(IRoleRepository roleRepository) : IRoleQueryService
{
    /**
     * <summary>
     *     Handle get all roles query
     * </summary>
     * <param name="query">The query object for getting all roles</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>The list of roles</returns>
     */
    public async Task<IEnumerable<Role>> Handle(GetAllRolesQuery query, CancellationToken cancellationToken)
    {
        return await roleRepository.ListAsync(cancellationToken);
    }
}
