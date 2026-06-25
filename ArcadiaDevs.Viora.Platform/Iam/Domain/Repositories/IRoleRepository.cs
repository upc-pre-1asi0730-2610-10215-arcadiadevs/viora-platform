using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;

/**
 * <summary>
 *     The role repository
 * </summary>
 * <remarks>
 *     This repository is used to manage roles
 * </remarks>
 */
public interface IRoleRepository : IBaseRepository<Role>
{
    /**
     * <summary>
     *     Find a role by name
     * </summary>
     * <param name="name">The role name to search</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>The role if found, otherwise null</returns>
     */
    Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /**
     * <summary>
     *     Check if a role exists by name
     * </summary>
     * <param name="name">The role name to search</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>True if the role exists, false otherwise</returns>
     */
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
}
