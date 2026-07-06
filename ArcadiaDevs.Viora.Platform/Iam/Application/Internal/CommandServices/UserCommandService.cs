using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Iam.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.OutboundServices;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain;
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
    IBillingContextFacade billingContextFacade,
    IClock clock)
{

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
