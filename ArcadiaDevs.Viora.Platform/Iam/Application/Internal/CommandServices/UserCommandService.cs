using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Iam.Application.Internal.CommandServices;

/**
 * <summary>
 *     Handles user authentication commands.
 * </summary>
 */
public class UserCommandService(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ITokenService tokenService,
    IHashingService hashingService,
    IRoleRepository roleRepository,
    IProfileContextFacade profileContextFacade,
    IStringLocalizer<ErrorMessages> errorLocalizer) : IUserCommandService
{
    /// <inheritdoc />
    public async Task<Result<User?, Error>> Handle(
        SignUpCommand command,
        CancellationToken cancellationToken)
    {
        // Validate password strength
        if (string.IsNullOrEmpty(command.Password) || command.Password.Length < 8)
            return new Result<User?, Error>.Failure(IamErrors.WeakPassword);

        // Check if username is already taken
        var exists = await userRepository.ExistsByUsernameAsync(command.Username, cancellationToken);
        if (exists)
            return new Result<User?, Error>.Failure(IamErrors.UsernameAlreadyTaken);

        // Validate required fields for profile provisioning
        if (string.IsNullOrWhiteSpace(command.Email))
            return new Result<User?, Error>.Failure(IamErrors.EmailRequired);

        if (string.IsNullOrWhiteSpace(command.FullName))
            return new Result<User?, Error>.Failure(IamErrors.FullNameRequired);

        // Resolve the role to assign. Omitted/blank defaults to "Grower"
        // (mirrors OS's Role.getDefaultRole() -> ROLE_GROWER). An explicit
        // role that no longer exists (e.g. the retired "Administrator") is
        // rejected before any password hashing or persistence happens.
        var roleName = string.IsNullOrWhiteSpace(command.Role) ? "Grower" : command.Role;
        var role = await roleRepository.FindByNameAsync(roleName, cancellationToken);
        if (role == null)
            return new Result<User?, Error>.Failure(IamErrors.InvalidRoleName);

        // Hash the password
        var passwordHash = hashingService.HashPassword(command.Password);

        // Create the user
        var user = new User(command.Username, passwordHash);
        user.Roles.Add(role);

        try
        {
            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new Result<User?, Error>.Failure(IamErrors.UserCreationFailed);
        }

        // Provision profile after user is durably persisted.
        // This is outside the try/catch — if profile creation fails, the user
        // still exists; EnsureProfile is idempotent so callers can retry.
        await profileContextFacade.EnsureProfile(
            user.Id, command.FullName, command.Email,
            ct: cancellationToken);

        return new Result<User?, Error>.Success(user);
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticatedUser, Error>> Handle(
        SignInCommand command,
        CancellationToken cancellationToken)
    {
        // Find user by username
        var user = await userRepository.FindByUsernameAsync(command.Username, cancellationToken);

        if (user == null)
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.InvalidCredentials);

        // Verify password
        if (!hashingService.VerifyPassword(command.Password, user.PasswordHash))
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.InvalidCredentials);

        // Generate token
        var token = tokenService.GenerateToken(user);

        return new Result<AuthenticatedUser, Error>.Success(
            new AuthenticatedUser(user.Id, user.Username, token));
    }

    /// <inheritdoc />
    public async Task<Result<User?, Error>> Handle(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        // Find the user
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return new Result<User?, Error>.Failure(IamErrors.UserNotFound);

        // Verify the current password against the stored hash. Matches OS's
        // ChangePasswordCommand contract exactly: bearerAuth-only, no
        // self-only/ownership guard (any authenticated caller may target any
        // userId) — an inherited contract risk, documented, not fixed here.
        if (!hashingService.VerifyPassword(command.CurrentPassword, user.PasswordHash))
            return new Result<User?, Error>.Failure(IamErrors.InvalidCurrentPassword);

        // Validate new password strength
        if (string.IsNullOrEmpty(command.NewPassword) || command.NewPassword.Length < 8)
            return new Result<User?, Error>.Failure(IamErrors.WeakPassword);

        // Apply the new password hash
        var newPasswordHash = hashingService.HashPassword(command.NewPassword);
        user.UpdatePasswordHash(newPasswordHash);

        try
        {
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new Result<User?, Error>.Failure(IamErrors.UserCreationFailed);
        }

        return new Result<User?, Error>.Success(user);
    }
}
