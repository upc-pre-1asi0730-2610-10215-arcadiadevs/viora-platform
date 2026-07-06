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

    /**
     * <summary>
     *     Handle change password command
     * </summary>
     * <param name="command">The change password command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result containing the updated user, or an error</returns>
     */
    Task<Result<User?, Error>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken);

    /**
     * <summary>
     *     Handle verify command
     * </summary>
     * <param name="command">The verify command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result containing the authenticated user and token, or an error</returns>
     */
    Task<Result<AuthenticatedUser, Error>> Handle(VerifyCommand command, CancellationToken cancellationToken);

    /**
     * <summary>
     *     Handle resend-verification command
     * </summary>
     * <param name="command">The resend-verification command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result containing the updated user, or an error</returns>
     */
    Task<Result<User?, Error>> Handle(ResendVerificationCommand command, CancellationToken cancellationToken);

    /**
     * <summary>
     *     Handle deactivate-user command
     * </summary>
     * <param name="command">The deactivate-user command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result containing the updated user, or an error</returns>
     */
    Task<Result<User?, Error>> Handle(DeactivateUserCommand command, CancellationToken cancellationToken);

    /**
     * <summary>
     *     Handle delete-user command
     * </summary>
     * <param name="command">The delete-user command</param>
     * <param name="cancellationToken">The cancellation token</param>
     * <returns>A result indicating whether the account was found and deleted</returns>
     */
    Task<Result<bool, Error>> Handle(DeleteUserCommand command, CancellationToken cancellationToken);
}
