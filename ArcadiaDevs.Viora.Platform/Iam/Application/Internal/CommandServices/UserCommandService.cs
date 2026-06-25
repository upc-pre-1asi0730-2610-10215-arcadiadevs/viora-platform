using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Resources.Errors;
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

        // Hash the password
        var passwordHash = hashingService.HashPassword(command.Password);

        // Create the user
        var user = new User(command.Username, passwordHash);

        try
        {
            await userRepository.AddAsync(user, cancellationToken);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new Result<User?, Error>.Failure(IamErrors.UserCreationFailed);
        }

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
}
