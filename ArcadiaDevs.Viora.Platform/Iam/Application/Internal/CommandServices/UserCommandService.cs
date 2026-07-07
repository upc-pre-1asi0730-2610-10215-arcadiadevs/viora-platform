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
    IVerificationTokenRepository verificationTokenRepository,
    IUserSessionRepository userSessionRepository,
    IEmailService emailService,
    IClock clock,
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

        // A specialist account needs a reachable contact number for producers
        // to coordinate interventions — required at sign-up, not optional
        // profile metadata, for this role only.
        if (string.Equals(roleName, "Specialist", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(command.Phone))
            return new Result<User?, Error>.Failure(IamErrors.SpecialistPhoneRequired);

        // Hash the password
        var passwordHash = hashingService.HashPassword(command.Password);

        // Create the user. Persists Email/FullName on Iam's own User (REQ-AUTH-1,
        // in addition to forwarding them to Profile below). Payment-first
        // onboarding: the account is created active (Verified=true) for every
        // role — sign-up is only reachable from the plan-selection screen, so
        // by the time this command runs the caller has already committed to a
        // plan; the app is gated behind an active subscription, not email
        // verification. Matches OS's `2f656f3` exactly (unconditional, not
        // scoped to any one role) — confirmed as the intended product model,
        // not a literal-vs-intent mismatch in OS's code.
        var user = new User(command.Username, passwordHash, command.Email, command.FullName, verified: true);
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
            phone: command.Phone,
            ct: cancellationToken);

        // REQ-REF-6: grant the referral reward to the code's OWNER (not this
        // new user) when a valid referral code was supplied. Same best-effort
        // placement as EnsureProfile above — outside the signup's core
        // try/catch, so a referral-processing issue can never make this
        // response falsely report signup failure. Lenient no-op if the code
        // is blank/unknown (isolated to this call site only).
        await billingContextFacade.GrantReferralRewardToCodeOwner(
            command.ReferralCode, user.Id, cancellationToken);

        // Payment-first onboarding: the account is already Verified=true above,
        // so there is no verification step to issue a token/email for anymore.
        // VerifyCommand/ResendVerificationCommand remain wired below for any
        // pre-existing unverified account, but are dead paths for any signup
        // from this point forward — matches OS's `2f656f3` exactly.
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

        // Gate order matches OS (UserCommandServiceImpl.java:89-95): credentials
        // -> Verified -> Active. A deactivated+unverified user must fail on
        // EmailNotVerified (422), not UserDisabled (403) — the previous WA
        // order (Active before Verified) was backwards and gave the opposite
        // result for that combination. Fixed 2026-07-05 per the OS-WA parity
        // re-audit (docs/os-wa-parity-audit-2026-07-05.md).
        if (!user.Verified)
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.EmailNotVerified);

        if (!user.Active)
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.UserDisabled);

        // Generate token
        var token = tokenService.GenerateToken(user);

        // REQ-SESS-1/REQ-SESS-4: demote any prior current session for this
        // user, then insert a fresh row — every sign-in creates a new session,
        // no upsert/dedup, unlimited concurrent sessions per user.
        var existingSessions = await userSessionRepository.FindByUserIdAsync(user.Id, cancellationToken);
        foreach (var existingSession in existingSessions.Where(s => s.IsCurrent))
        {
            existingSession.Demote();
            userSessionRepository.Update(existingSession);
        }

        var userAgent = string.IsNullOrWhiteSpace(command.UserAgent) ? "Unknown" : command.UserAgent;
        var session = new UserSession(user.Id, userAgent, clock.UtcNow, isCurrent: true);
        await userSessionRepository.AddAsync(session, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

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

    /// <inheritdoc />
    public async Task<Result<AuthenticatedUser, Error>> Handle(
        VerifyCommand command,
        CancellationToken cancellationToken)
    {
        var verificationToken = await verificationTokenRepository.FindByTokenAsync(command.Token, cancellationToken);
        if (verificationToken == null)
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.VerificationTokenNotFound);

        var consumeResult = verificationToken.Consume(clock.UtcNow);
        if (consumeResult is Result<Unit, Error>.Failure consumeFailure)
            return new Result<AuthenticatedUser, Error>.Failure(consumeFailure.Error);

        var user = await userRepository.FindByIdAsync(verificationToken.UserId, cancellationToken);
        if (user == null)
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.UserNotFound);

        user.MarkVerified();

        try
        {
            verificationTokenRepository.Update(verificationToken);
            userRepository.Update(user);
            await unitOfWork.CompleteAsync(cancellationToken);
        }
        catch (Exception)
        {
            return new Result<AuthenticatedUser, Error>.Failure(IamErrors.UserCreationFailed);
        }

        // Auto sign-in on success (matches OS). Unlike SignInCommand, this does
        // NOT record a UserSession — only SignInCommand records sessions.
        var token = tokenService.GenerateToken(user);
        return new Result<AuthenticatedUser, Error>.Success(
            new AuthenticatedUser(user.Id, user.Username, token));
    }

    /// <inheritdoc />
    public async Task<Result<User?, Error>> Handle(
        ResendVerificationCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByUsernameAsync(command.Username, cancellationToken);
        if (user == null)
            return new Result<User?, Error>.Failure(IamErrors.UserNotFound);

        if (user.Verified)
            return new Result<User?, Error>.Failure(IamErrors.EmailAlreadyVerified);

        // Resend does NOT invalidate prior outstanding tokens (matches OS
        // exactly — accepted minor gap, not fixed).
        var verificationToken = VerificationToken.IssueEmailVerification(user.Id, clock.UtcNow);
        await verificationTokenRepository.AddAsync(verificationToken, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);

        await emailService.SendVerificationEmailAsync(
            user.Email ?? string.Empty, user.FullName ?? user.Username, verificationToken.Token, cancellationToken);

        return new Result<User?, Error>.Success(user);
    }

    /// <inheritdoc />
    public async Task<Result<User?, Error>> Handle(
        DeactivateUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return new Result<User?, Error>.Failure(IamErrors.UserNotFound);

        var deactivateResult = user.Deactivate();
        if (deactivateResult is Result<Unit, Error>.Failure deactivateFailure)
            return new Result<User?, Error>.Failure(deactivateFailure.Error);

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

    /// <inheritdoc />
    public async Task<Result<bool, Error>> Handle(
        DeleteUserCommand command,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(command.UserId, cancellationToken);
        if (user == null)
            return new Result<bool, Error>.Failure(IamErrors.UserNotFound);

        // Matches OS's UserCommandServiceImpl.handle(DeleteUserCommand) exactly:
        // remove the account's Iam footprint and its profile, in this order
        // (sessions, verification tokens, profile, then the user itself).
        // Domain data owned by the user elsewhere (plots, billing, …) becomes
        // unreachable once the user is gone — not cascaded here, same as OS.
        var sessions = await userSessionRepository.FindByUserIdAsync(user.Id, cancellationToken);
        foreach (var session in sessions)
            userSessionRepository.Remove(session);

        var verificationTokens = await verificationTokenRepository.FindByUserIdAsync(user.Id, cancellationToken);
        foreach (var token in verificationTokens)
            verificationTokenRepository.Remove(token);

        await profileContextFacade.DeleteByUserIdAsync(user.Id, cancellationToken);

        userRepository.Remove(user);
        await unitOfWork.CompleteAsync(cancellationToken);

        return new Result<bool, Error>.Success(true);
    }
}
