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

public class UserCommandServiceTests
{
    private readonly IUserRepository    _userRepository    = Substitute.For<IUserRepository>();
    private readonly IUnitOfWork        _unitOfWork        = Substitute.For<IUnitOfWork>();
    private readonly ITokenService      _tokenService      = Substitute.For<ITokenService>();
    private readonly IHashingService    _hashingService    = Substitute.For<IHashingService>();
    private readonly IRoleRepository    _roleRepository    = Substitute.For<IRoleRepository>();
    private readonly IProfileContextFacade _profileContextFacade = Substitute.For<IProfileContextFacade>();
    private readonly IBillingContextFacade _billingContextFacade = Substitute.For<IBillingContextFacade>();
    private readonly IClock             _clock             = Substitute.For<IClock>();
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer =
        Substitute.For<IStringLocalizer<ErrorMessages>>();
    private readonly UserCommandService _sut;
    

    [Fact]
    public async Task Handle_SignUp_ReturnsWeakPassword_WhenPasswordShorterThan8Chars()
    {
        // Explicit stub per T4: pin the existence check to "user does not exist"
        // so a future refactor that moves the password-length check AFTER the
        // existence check still exercises the same code branch in this test.
        _userRepository.ExistsByUsernameAsync("alice", Arg.Any<CancellationToken>())
                       .Returns(false);

        var command = new SignUpCommand("alice", "short", "alice@example.com", "Alice Smith");   // 5 chars; < 8

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

        var command = new SignUpCommand("alice", "long-enough-password", "alice@example.com", "Alice Smith");   // 18 chars; >= 8

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


        // CRITICAL: token must never be generated for an unknown user
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<User>());
        _hashingService.DidNotReceive().VerifyPassword(Arg.Any<string>(), Arg.Any<string>());
    }

    // REQ-1 (spec obs #156, design obs #155 Decision 1): SignUp gains an optional
    // Role field. Omitted/blank defaults to "Grower" (WA's equivalent of OS's
    // Role.getDefaultRole() -> ROLE_GROWER). An explicit valid role is honored.
    // An explicit invalid role returns IamErrors.InvalidRoleName.

    [Fact]
    public async Task Handle_SignUpCommand_OmittedRole_DefaultsToGrower()
    {
        _userRepository.ExistsByUsernameAsync("alice", Arg.Any<CancellationToken>())
                       .Returns(false);
        var growerRole = ((Result<Role, Error>.Success)Role.Create("Grower")).Value;
        _roleRepository.FindByNameAsync("Grower", Arg.Any<CancellationToken>())
                       .Returns(growerRole);
        _hashingService.HashPassword(Arg.Any<string>()).Returns("hashed");

        var command = new SignUpCommand("alice", "long-enough-password", "alice@example.com", "Alice Smith", Role: null);

        await _roleRepository.Received(1).FindByNameAsync("Grower", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SignUpCommand_ExplicitValidRole_AssignsRole()
    {
        _userRepository.ExistsByUsernameAsync("bob", Arg.Any<CancellationToken>())
                       .Returns(false);
        var specialistRole = ((Result<Role, Error>.Success)Role.Create("Specialist")).Value;
        _roleRepository.FindByNameAsync("Specialist", Arg.Any<CancellationToken>())
                       .Returns(specialistRole);
        _hashingService.HashPassword(Arg.Any<string>()).Returns("hashed");

        var command = new SignUpCommand("bob", "long-enough-password", "bob@example.com", "Bob Jones", Role: "Specialist");
        
    }

    [Fact]
    public async Task Handle_SignUpCommand_InvalidRoleString_ReturnsInvalidRoleName()
    {
        _userRepository.ExistsByUsernameAsync("carol", Arg.Any<CancellationToken>())
                       .Returns(false);
        // "Administrator" no longer exists as a seeded role — FindByNameAsync misses.
        _roleRepository.FindByNameAsync("Administrator", Arg.Any<CancellationToken>())
                       .Returns((Role?)null);

        var command = new SignUpCommand("carol", "long-enough-password", "carol@example.com", "Carol Davis", Role: "Administrator");
        
        // CRITICAL: hashing/persistence must never run for an unresolvable role
        // (mirrors the weak-password/username-taken early-return guards above).
        _hashingService.DidNotReceive().HashPassword(Arg.Any<string>());
        _ = _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }
}
