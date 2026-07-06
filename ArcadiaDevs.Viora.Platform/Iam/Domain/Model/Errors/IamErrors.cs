using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Errors;

public static class IamErrors
{
    public static readonly Error InvalidCredentials =
        new("Iam.InvalidCredentials", "Invalid username or password.");

    public static readonly Error UsernameAlreadyTaken =
        new("Iam.UsernameAlreadyTaken", "The specified username is already taken.");

    public static readonly Error UserCreationFailed =
        new("Iam.UserCreationFailed", "An error occurred while creating the user.");

    public static readonly Error WeakPassword =
        new("Iam.WeakPassword", "The password must be at least 8 characters long.");

    public static readonly Error EmailRequired =
        new("Iam.EmailRequired", "Email is required.");

    public static readonly Error FullNameRequired =
        new("Iam.FullNameRequired", "Full name is required.");

    public static readonly Error UserNotFound =
        new("Iam.UserNotFound", "The specified user was not found.");

    public static readonly Error TokenRequired =
        new("Iam.TokenRequired", "An authorization token is required to access this resource.");

    public static readonly Error TokenMalformed =
        new("Iam.TokenMalformed", "The authorization token is malformed.");

    public static readonly Error TokenInvalid =
        new("Iam.TokenInvalid", "The authorization token is invalid.");

    public static readonly Error TokenExpired =
        new("Iam.TokenExpired", "The authorization token has expired.");

    public static readonly Error InvalidRoleName =
        new("Iam.InvalidRoleName", "The specified role name is not valid.");

    public static readonly Error InvalidCurrentPassword =
        new("Iam.InvalidCurrentPassword", "The current password is incorrect.");

    // Reserved for cross-slice
    public static readonly Error InsufficientRole =
        new("Iam.InsufficientRole", "The user does not have the required role to access this resource.");

    public static readonly Error UserDisabled =
        new("Iam.UserDisabled", "The user account is disabled.");

    public static readonly Error UserAlreadyDeactivated =
        new("Iam.UserAlreadyDeactivated", "The user account is already deactivated.");

    public static readonly Error EmailNotVerified =
        new("Iam.EmailNotVerified", "The account's email has not been verified.");

    public static readonly Error EmailAlreadyVerified =
        new("Iam.EmailAlreadyVerified", "The account's email is already verified.");

    public static readonly Error InvalidAccountStateUpdate =
        new("Iam.InvalidAccountStateUpdate", "The requested account state update is not valid.");

    public static readonly Error VerificationTokenExpiredOrConsumed =
        new("Iam.VerificationTokenExpiredOrConsumed", "The verification token is expired or has already been used.");

    public static readonly Error VerificationTokenNotFound =
        new("Iam.VerificationTokenNotFound", "The specified verification token was not found.");

    public static readonly Error SessionNotFound =
        new("Iam.SessionNotFound", "The specified session was not found.");

    public static readonly Error CannotRevokeCurrentSession =
        new("Iam.CannotRevokeCurrentSession", "The current session cannot be revoked.");
}
