using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;

/**
 * <summary>
 *     The user command service interface
 * </summary>
 * <remarks>
 *     This service contract specifies handling behavior used to execute user commands
 * </remarks>
 */
public interface IUserCommandService
{
    /**
     * <summary>
     *     Handle sign up command
     * </summary>
     * <param name="command">The sign up command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result indicating success or failure</returns>
     */
    Task<Result<User?, Error>> Handle(SignUpCommand command, CancellationToken cancellationToken);

    /**
     * <summary>
     *     Handle sign in command
     * </summary>
     * <param name="command">The sign in command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result containing the authenticated user and token, or an error</returns>
     */
    Task<Result<AuthenticatedUser, Error>> Handle(SignInCommand command, CancellationToken cancellationToken);
}
