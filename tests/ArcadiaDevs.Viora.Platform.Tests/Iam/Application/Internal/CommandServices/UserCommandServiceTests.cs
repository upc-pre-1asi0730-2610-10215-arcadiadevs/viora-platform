using ArcadiaDevs.Viora.Platform.Iam.Application.Internal.CommandServices;
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

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Application.Internal.CommandServices;

public class UserCommandServiceTests
{
    private readonly IUserRepository    _userRepository    = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork        _unitOfWork        = Substitute.For<IUnitOfWork>();
    private readonly ITokenService      _tokenService      = Substitute.For<ITokenService>();
    private readonly IHashingService    _hashingService    = Substitute.For<IHashingService>();
    private readonly IRoleRepository    _roleRepository    = Substitute.For<IRoleRepository>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer =
        Substitute.For<IStringLocalizer<ErrorMessages>>();
    private readonly UserCommandService _sut;

    public UserCommandServiceTests()
    {
        _sut = new UserCommandService(
            _userRepository, _unitOfWork, _tokenService, _hashingService, _roleRepository, _errorLocalizer);
    }

    [Fact]
    public async Task Handle_SignUp_ReturnsWeakPassword_WhenPasswordShorterThan8Chars()
    {
        // Explicit stub per T4: pin the existence check to "user does not exist"
        // so a future refactor that moves the password-length check AFTER the
        // existence check still exercises the same code branch in this test.
        _userRepository.ExistsByUsernameAsync("alice", Arg.Any<CancellationToken>())
                       .Returns(false);

        var command = new SignUpCommand("alice", "short");   // 5 chars; < 8

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.WeakPassword, ((Result<User?, Error>.Failure)result).Error);
        Assert.Equal("Iam.WeakPassword", ((Result<User?, Error>.Failure)result).Error.Code);

        // CRITICAL: hashing must never run on a weak password (timing / branch-leak guard)
        _hashingService.DidNotReceive().HashPassword(Arg.Any<string>());
        // Discard with _ = : AddAsync returns Task, otherwise the compiler emits CS4014.
        _ = _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SignUp_ReturnsUsernameAlreadyTaken_WhenUserExists()
    {
        // NEW in v1.3 (spec v1.3 Req 9b). The stub drives execution PAST the
        // password-length check at UserCommandService.cs:36-37 (the password
        // "long-enough-password" is 18 chars, well above the 8-char minimum)
        // and INTO the ExistsByUsernameAsync branch at UserCommandService.cs:40-42.
        _userRepository.ExistsByUsernameAsync("alice", Arg.Any<CancellationToken>())
                       .Returns(true);   // note: TRUE — the opposite of the weak-password test

        var command = new SignUpCommand("alice", "long-enough-password");   // 18 chars; >= 8

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.UsernameAlreadyTaken, ((Result<User?, Error>.Failure)result).Error);
        Assert.Equal("Iam.UsernameAlreadyTaken", ((Result<User?, Error>.Failure)result).Error.Code);

        // CRITICAL: user creation must never run on a taken username
        // (existence-check early-return guard; if production ever reorders the
        // checks so AddAsync runs before the existence check, this guard catches it).
        // Discard with _ = : AddAsync returns Task, otherwise the compiler emits CS4014.
        _ = _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());

        // CRITICAL: hashing must never run if the existence check fails
        // (branch-leak guard — same rationale as the weak-password test).
        _hashingService.DidNotReceive().HashPassword(Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_SignIn_ReturnsInvalidCredentials_WhenUserNotFound()
    {
        _userRepository.FindByUsernameAsync("alice", Arg.Any<CancellationToken>())
                       .Returns((User?)null);

        var command = new SignInCommand("alice", "any-password");

        var result = await _sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(IamErrors.InvalidCredentials, ((Result<AuthenticatedUser, Error>.Failure)result).Error);
        Assert.Equal("Iam.InvalidCredentials", ((Result<AuthenticatedUser, Error>.Failure)result).Error.Code);

        // CRITICAL: token must never be generated for an unknown user
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
        _hashingService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }
}
