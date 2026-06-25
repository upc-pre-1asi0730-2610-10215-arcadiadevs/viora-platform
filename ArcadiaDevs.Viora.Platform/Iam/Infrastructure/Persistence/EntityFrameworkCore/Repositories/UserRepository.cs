using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Configuration;
using ArcadiaDevs.Viora.Platform.Shared.Infrastructure.Persistence.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Persistence.EntityFrameworkCore.Repositories;

/**
 * <summary>
 *     The user repository
 * </summary>
 * <remarks>
 *     This repository is used to manage users
 * </remarks>
 */
public class UserRepository(AppDbContext context) : BaseRepository<User>(context), IUserRepository
{
    /**
     * <summary>
     *     Find a user by username
     * </summary>
     * <param name="username">The username to search</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>The user</returns>
     */
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await Context.Set<User>()
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(user => user.Username.Equals(username), cancellationToken);
    }

    /**
     * <summary>
     *     Check if a user exists by username
     * </summary>
     * <param name="username">The username to search</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>True if the user exists, false otherwise</returns>
     */
    public async Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return await Context.Set<User>().AnyAsync(user => user.Username.Equals(username), cancellationToken);
    }
}
