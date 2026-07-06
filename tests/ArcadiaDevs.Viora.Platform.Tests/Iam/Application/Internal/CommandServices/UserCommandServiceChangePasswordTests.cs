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
///     Unit tests for <see cref="UserCommandService.Handle(ChangePasswordCommand, CancellationToken)"/>.
/// </summary>
public class UserCommandServiceChangePasswordTests
{
    private readonly IUserRepository    _userRepository    = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork        _unitOfWork        = Substitute.For<IUnitOfWork>();
    private readonly ITokenService      _tokenService      = Substitute.For<ITokenService>();
    private readonly IHashingService    _hashingService    = Substitute.For<IHashingService>();
    private readonly IRoleRepository    _roleRepository    = Substitute.For<IRoleRepository>();
    private readonly IProfileContextFacade _profileContextFacade = Substitute.For<IProfileContextFacade>();
    private readonly IBillingContextFacade _billingContextFacade = Substitute.For<IBillingContextFacade>();
    private readonly IVerificationTokenRepository _verificationTokenRepository =
        Substitute.For<IVerificationTokenRepository>();
    private readonly IUserSessionRepository _userSessionRepository = Substitute.For<IUserSessionRepository>();
    private readonly IEmailService      _emailService      = Substitute.For<IEmailService>();
    private readonly IClock             _clock             = Substitute.For<IClock>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer =
        Substitute.For<IStringLocalizer<ErrorMessages>>();
    private readonly UserCommandService _sut;

    public UserCommandServiceChangePasswordTests()
    {
        _sut = new UserCommandService(
            _userRepository, _unitOfWork, _tokenService, _hashingService, _roleRepository, _profileContextFacade,
            _billingContextFacade, _verificationTokenRepository, _userSessionRepository, _emailService, _clock,
            _errorLocalizer);
    }

    /// <summary>
    ///     Happy path: correct current password + new password meets strength requirement → success.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Database", "InMemory")]
    public async Task Handle_ChangePassword_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var user = new User("alice", "old-hash");
        _userRepository.FindByIdAsync(1, Arg.Any<CancellationToken>())
                       .Returns(user);
        _hashingService.VerifyPassword("current-password", "old-hash")
                       .Returns(true);
        _hashingService.HashPassword("new-password-long-enough")
                       .Returns("new-hash");

        var command = new ChangePasswordCommand(1, "current-password", "new-password-long-enough");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedUser = ((Result<User?, Error>.Success)result).Value;
        Assert.NotNull(updatedUser);
        Assert.Equal("new-hash", updatedUser!.PasswordHash);

        _userRepository.Received(1).Update(user);
        await _unitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     Wrong current password → <see cref="IamErrors.InvalidCurrentPassword"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Database", "InMemory")]
    public async Task Handle_ChangePassword_InvalidCurrentPassword_ReturnsFailure()
    {
        // Arrange
        var user = new User("alice", "old-hash");
        _userRepository.FindByIdAsync(1, Arg.Any<CancellationToken>())
                       .Returns(user);
        _hashingService.VerifyPassword("wrong-password", "old-hash")
                       .Returns(false);

        var command = new ChangePasswordCommand(1, "wrong-password", "new-password-long-enough");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.InvalidCurrentPassword, ((Result<User?, Error>.Failure)result).Error);
        Assert.Equal("Iam.InvalidCurrentPassword", ((Result<User?, Error>.Failure)result).Error.Code);

        // Password hashing must never run when current password is wrong
        _hashingService.DidNotReceive().HashPassword(Arg.Any<string>());
        _userRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    /// <summary>
    ///     New password below 8-char minimum → <see cref="IamErrors.WeakPassword"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Database", "InMemory")]
    public async Task Handle_ChangePassword_WeakPassword_ReturnsFailure()
    {
        // Arrange
        var user = new User("alice", "old-hash");
        _userRepository.FindByIdAsync(1, Arg.Any<CancellationToken>())
                       .Returns(user);
        _hashingService.VerifyPassword("current-password", "old-hash")
                       .Returns(true);

        var command = new ChangePasswordCommand(1, "current-password", "short"); // 5 chars < 8

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.WeakPassword, ((Result<User?, Error>.Failure)result).Error);
        Assert.Equal("Iam.WeakPassword", ((Result<User?, Error>.Failure)result).Error.Code);

        // Hashing must never run on a weak new password
        _hashingService.DidNotReceive().HashPassword(Arg.Any<string>());
        _userRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    /// <summary>
    ///     User not found → <see cref="IamErrors.UserNotFound"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Database", "InMemory")]
    public async Task Handle_ChangePassword_UserNotFound_ReturnsFailure()
    {
        // Arrange
        _userRepository.FindByIdAsync(999, Arg.Any<CancellationToken>())
                       .Returns((User?)null);

        var command = new ChangePasswordCommand(999, "current-password", "new-password-long-enough");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.UserNotFound, ((Result<User?, Error>.Failure)result).Error);
        Assert.Equal("Iam.UserNotFound", ((Result<User?, Error>.Failure)result).Error.Code);

        // Verify must never run for a non-existent user
        _hashingService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
        _hashingService.DidNotReceive().HashPassword(Arg.Any<string>());
    }
}
