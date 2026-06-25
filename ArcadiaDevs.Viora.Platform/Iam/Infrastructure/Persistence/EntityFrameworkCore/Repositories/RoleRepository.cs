using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/**
 * <summary>
 *     The role repository
 * </summary>
 * <remarks>
 *     This repository is used to manage roles
 * </remarks>
 */
public class RoleRepository(AppDbContext context) : BaseRepository<Role>(context), IRoleRepository
{
    /**
     * <summary>
     *     Find a role by name
     * </summary>
     * <param name="name">The role name to search</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>The role if found, otherwise null</returns>
     */
    public async Task<Role?> FindByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Role>()
            .FirstOrDefaultAsync(role => role.Name.Equals(name), cancellationToken);
    }

    /**
     * <summary>
     *     Check if a role exists by name
     * </summary>
     * <param name="name">The role name to search</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>True if the role exists, false otherwise</returns>
     */
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await Context.Set<Role>()
            .AnyAsync(role => role.Name.Equals(name), cancellationToken);
    }
}
