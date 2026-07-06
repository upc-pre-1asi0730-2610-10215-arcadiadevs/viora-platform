using System.Reflection;
using ArcadiaDevs.Viora.Platform.Billing.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.CommandServices;
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
using Microsoft.Extensions.Localization;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Application.Internal.CommandServices;

/// <summary>
///     Unit tests for <see cref="UserCommandService.Handle(DeleteUserCommand, CancellationToken)" />
///     (priority-3 coverage: account deletion cascade — REQ-DEL, backs the
///     Settings › Security "Delete account" action / DELETE /users/{userId}).
///     Template B: command service with NSubstitute mocks (mirrors
///     PlotQueryServiceTests / UserCommandServiceTests constructor-injection style).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class UserCommandServiceDeleteAccountTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IHashingService _hashingService = Substitute.For<IHashingService>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IProfileContextFacade _profileContextFacade = Substitute.For<IProfileContextFacade>();
    private readonly IBillingContextFacade _billingContextFacade = Substitute.For<IBillingContextFacade>();
    private readonly IVerificationTokenRepository _verificationTokenRepository =
        Substitute.For<IVerificationTokenRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer =
        Substitute.For<IStringLocalizer<ErrorMessages>>();
    private readonly UserCommandService _sut;

    public UserCommandServiceDeleteAccountTests()
    {
        _sut = new UserCommandService(
            _userRepository, _unitOfWork, _tokenService, _hashingService, _roleRepository, _profileContextFacade,
            _billingContextFacade, _verificationTokenRepository, _userSessionRepository, _emailService, _clock,
            _errorLocalizer);
    }

    /// <summary>
    ///     Sets a read-only <c>{ get; }</c> auto-property's compiler-generated
    ///     backing field via reflection — the same trick EF Core uses at
    ///     materialization time (mirrors UsersControllerGetMeTests.SetBackingField).
    /// </summary>
    private static T SetIdBackingField<T>(T entity, string propertyName, int id)
    {
        var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(entity, id);
        return entity;
    }

    private static User BuildUser(int id, string username = "alice") =>
        SetIdBackingField(
            new User(username, "hashed-password", $"{username}@example.com", "Alice Smith"),
            nameof(User.Id), id);

    private static UserSession BuildSession(int id, int userId) =>
        SetIdBackingField(
            new UserSession(userId, "Mozilla/5.0", DateTime.UtcNow, isCurrent: true),
            nameof(UserSession.Id), id);

    private static VerificationToken BuildVerificationToken(int id, int userId) =>
        SetIdBackingField(
            VerificationToken.IssueEmailVerification(userId, DateTime.UtcNow),
            nameof(VerificationToken.Id), id);

    /// <summary>
    ///     GIVEN a non-existent user id
    ///     WHEN <see cref="UserCommandService.Handle(DeleteUserCommand, CancellationToken)" /> is called
    ///     THEN <see cref="IamErrors.UserNotFound" /> is returned and no cascade side effects run.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteUser_UserNotFound_ReturnsNotFound()
    {
        // GIVEN no user exists for id 999
        _userRepository.FindByIdAsync(999, Arg.Any<CancellationToken>()).Returns((User?)null);

        var command = new DeleteUserCommand(999);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN UserNotFound is returned
        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.UserNotFound, ((Result<bool, Error>.Failure)result).Error);

        // AND no cascade side effects ran (branch-leak guard)
        _ = _userSessionRepository.DidNotReceive().FindByUserIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _ = _verificationTokenRepository.DidNotReceive().FindByUserIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _profileContextFacade.DidNotReceive().DeleteByUserIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        _userRepository.DidNotReceive().Remove(Arg.Any<User>());
        await _unitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN an existing user with sessions, verification tokens, and a profile
    ///     WHEN <see cref="UserCommandService.Handle(DeleteUserCommand, CancellationToken)" /> is called
    ///     THEN all sessions are removed via <see cref="IUserSessionRepository.Remove" />,
    ///     all verification tokens are removed via <see cref="IVerificationTokenRepository.Remove" />,
    ///     the profile is deleted via <see cref="IProfileContextFacade.DeleteByUserIdAsync" />,
    ///     and finally the user itself is removed — all scoped to the correct user id.
    /// </summary>
    [Fact]
    public async Task Handle_DeleteUser_CascadesSessionsTokensAndProfile()
    {
        // GIVEN user 42 with 2 sessions and 2 verification tokens
        var user = BuildUser(42);
        _userRepository.FindByIdAsync(42, Arg.Any<CancellationToken>()).Returns(user);

        var session1 = BuildSession(1, 42);
        var session2 = BuildSession(2, 42);
        _userSessionRepository.FindByUserIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<UserSession> { session1, session2 });

        var token1 = BuildVerificationToken(1, 42);
        var token2 = BuildVerificationToken(2, 42);
        _verificationTokenRepository.FindByUserIdAsync(42, Arg.Any<CancellationToken>())
            .Returns(new List<VerificationToken> { token1, token2 });

        var command = new DeleteUserCommand(42);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the result is success
        Assert.True(result.IsSuccess);
        Assert.True(((Result<bool, Error>.Success)result).Value);

        // AND both sessions were removed
        _userSessionRepository.Received(1).Remove(session1);
        _userSessionRepository.Received(1).Remove(session2);

        // AND both verification tokens were removed
        _verificationTokenRepository.Received(1).Remove(token1);
        _verificationTokenRepository.Received(1).Remove(token2);

        // AND the profile was deleted for the correct user id
        await _profileContextFacade.Received(1).DeleteByUserIdAsync(42, Arg.Any<CancellationToken>());

        // AND the user itself was removed and the unit of work committed
        _userRepository.Received(1).Remove(user);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN an existing user with no sessions and no verification tokens
    ///     WHEN <see cref="UserCommandService.Handle(DeleteUserCommand, CancellationToken)" /> is called
    ///     THEN the cascade is a no-op for sessions/tokens but the profile facade is still
    ///     invoked and the user is still removed (empty-collection edge case).
    /// </summary>
    [Fact]
    public async Task Handle_DeleteUser_NoSessionsOrTokens_StillDeletesProfileAndUser()
    {
        // GIVEN user 7 with no sessions and no verification tokens
        var user = BuildUser(7, "bob");
        _userRepository.FindByIdAsync(7, Arg.Any<CancellationToken>()).Returns(user);
        _userSessionRepository.FindByUserIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<UserSession>());
        _verificationTokenRepository.FindByUserIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<VerificationToken>());

        var command = new DeleteUserCommand(7);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the result is still success
        Assert.True(result.IsSuccess);

        // AND no session/token Remove calls happened (nothing to remove)
        _userSessionRepository.DidNotReceive().Remove(Arg.Any<UserSession>());
        _verificationTokenRepository.DidNotReceive().Remove(Arg.Any<VerificationToken>());

        // AND the profile facade and user removal still ran for user 7
        await _profileContextFacade.Received(1).DeleteByUserIdAsync(7, Arg.Any<CancellationToken>());
        _userRepository.Received(1).Remove(user);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }
}
