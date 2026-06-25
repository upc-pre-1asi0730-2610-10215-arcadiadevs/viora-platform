namespace ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;

/**
 * <summary>
 *     ACL facade for cross-context Iam queries.
 * </summary>
 * <remarks>
 *     Other bounded contexts (Agronomic, Surveillance) consume this interface
 *     to check user existence and roles without depending on Iam aggregates
 *     or persistence types.
 * </remarks>
 */
public interface IIamContextFacade
{
    /**
     * <summary>
     *     Check whether a user exists by their id
     * </summary>
     * <param name="userId">The user id</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns><c>true</c> if the user exists, <c>false</c> otherwise (does not throw)</returns>
     */
    Task<bool> ExistsUserAsync(int userId, CancellationToken cancellationToken = default);

    /**
     * <summary>
     *     Get the role names assigned to a user
     * </summary>
     * <param name="userId">The user id</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>The list of role names (empty list if user not found; never <c>null</c>)</returns>
     */
    Task<IReadOnlyList<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
}
