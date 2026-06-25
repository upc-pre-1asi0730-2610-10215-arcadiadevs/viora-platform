using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.Acl;

/**
 * <summary>
 *     ACL facade implementation that delegates to <see cref="IUserRepository"/>.
 * </summary>
 * <remarks>
 *     This is the sole cross-context boundary for Iam. Return types are
 *     <c>bool</c> and <c>IReadOnlyList&lt;string&gt;</c> — no Iam aggregate
 *     or entity type leaks across the boundary.
 * </remarks>
 */
public class IamContextFacade(IUserRepository userRepository) : IIamContextFacade
{
    /// <inheritdoc />
    public async Task<bool> ExistsUserAsync(int userId, CancellationToken ct = default)
    {
        var user = await userRepository.FindByIdAsync(userId, ct);
        return user is not null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUserRolesAsync(int userId, CancellationToken ct = default)
    {
        return await userRepository.GetRolesByUserIdAsync(userId, ct);
    }
}
