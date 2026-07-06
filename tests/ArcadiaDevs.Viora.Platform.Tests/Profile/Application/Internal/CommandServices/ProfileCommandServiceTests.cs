using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Profile.Application.Internal.CommandServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Tests.Profile.Application.Internal.CommandServices;

/// <summary>
///     Unit tests for <see cref="ProfileCommandService"/>.
///     Covers OS-parity <c>photoUrl</c> persistence and the Profile-&gt;Iam
///     full-name sync round trip via <see cref="IIamContextFacade.UpdateFullNameAsync"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class ProfileCommandServiceTests
{
    private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
    private readonly IIamContextFacade _iamContextFacade = Substitute.For<IIamContextFacade>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ProfileCommandService _sut;

    public ProfileCommandServiceTests()
    {
        _sut = new ProfileCommandService(_profileRepository, _iamContextFacade, _unitOfWork);
    }

    /// <summary>
    ///     GIVEN no existing profile for the user
    ///     WHEN a <see cref="CreateOrUpdateProfileCommand"/> with a PhotoUrl is handled
    ///     THEN the new profile is created with the PhotoUrl set (OS parity — Profile.photoUrl)
    ///     and persisted via <see cref="IProfileRepository.AddAsync"/>.
    /// </summary>
    [Fact]
    public async Task Handle_NewProfile_WithPhotoUrl_PersistsPhotoUrl()
    {
        // GIVEN no existing profile for user 1
        _profileRepository.FindByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns((ProfileAggregate?)null);

        var command = new CreateOrUpdateProfileCommand(
            UserId: 1,
            FullName: "Jane Doe",
            Email: "jane@example.com",
            Phone: null,
            JobTitle: null,
            Language: null,
            Location: null,
            SpecialtyArea: null,
            PhotoUrl: "https://cdn.example.com/avatar.jpg");

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the created profile carries the PhotoUrl and was persisted via AddAsync
        Assert.True(result.IsSuccess);
        var (profile, created) = ((Result<(ProfileAggregate Profile, bool Created), Error>.Success)result).Value;
        Assert.True(created);
        Assert.Equal("https://cdn.example.com/avatar.jpg", profile.PhotoUrl);
        await _profileRepository.Received(1).AddAsync(
            Arg.Is<ProfileAggregate>(p => p.PhotoUrl == "https://cdn.example.com/avatar.jpg"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN an existing profile with no photo
    ///     WHEN a <see cref="CreateOrUpdateProfileCommand"/> with a new PhotoUrl is handled
    ///     THEN the existing profile is updated with the PhotoUrl (null-safe partial-update
    ///     semantics — <see cref="ProfileAggregate.ApplyUpdate"/>) and persisted via
    ///     <see cref="IProfileRepository.Update"/>.
    /// </summary>
    [Fact]
    public async Task Handle_ExistingProfile_WithPhotoUrl_UpdatesPhotoUrl()
    {
        // GIVEN an existing profile for user 1 with no photo
        var existing = new ProfileAggregate(1, ProfileRole.Producer, "Jane Doe", "jane@example.com");
        _profileRepository.FindByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreateOrUpdateProfileCommand(
            UserId: 1,
            FullName: null,
            Email: null,
            Phone: null,
            JobTitle: null,
            Language: null,
            Location: null,
            SpecialtyArea: null,
            PhotoUrl: "https://cdn.example.com/new-avatar.jpg");

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the existing profile is updated in place with the new PhotoUrl
        Assert.True(result.IsSuccess);
        var (profile, created) = ((Result<(ProfileAggregate Profile, bool Created), Error>.Success)result).Value;
        Assert.False(created);
        Assert.Equal("https://cdn.example.com/new-avatar.jpg", profile.PhotoUrl);
        _profileRepository.Received(1).Update(Arg.Is<ProfileAggregate>(p => p.PhotoUrl == "https://cdn.example.com/new-avatar.jpg"));
    }

    /// <summary>
    ///     GIVEN an existing profile
    ///     WHEN a <see cref="CreateOrUpdateProfileCommand"/> with a non-blank FullName is handled
    ///     THEN <see cref="IIamContextFacade.UpdateFullNameAsync"/> is called with the same
    ///     userId and the trimmed full name, keeping the Iam account display name in sync
    ///     with the profile (matches OS's ProfileCommandServiceImpl.handle).
    /// </summary>
    [Fact]
    public async Task Handle_FullNameProvided_CallsIamContextFacade_UpdateFullNameAsync()
    {
        // GIVEN an existing profile for user 1
        var existing = new ProfileAggregate(1, ProfileRole.Producer, "Old Name", "jane@example.com");
        _profileRepository.FindByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreateOrUpdateProfileCommand(
            UserId: 1,
            FullName: "  New Name  ",
            Email: null,
            Phone: null,
            JobTitle: null,
            Language: null,
            Location: null,
            SpecialtyArea: null,
            PhotoUrl: null);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the Iam facade is invoked once with the trimmed full name for the same user
        Assert.True(result.IsSuccess);
        await _iamContextFacade.Received(1).UpdateFullNameAsync(1, "New Name", Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN an existing profile
    ///     WHEN a <see cref="CreateOrUpdateProfileCommand"/> is handled with a null FullName
    ///     THEN <see cref="IIamContextFacade.UpdateFullNameAsync"/> is NOT called — production
    ///     guards the sync with <c>string.IsNullOrWhiteSpace(command.FullName)</c>, independent
    ///     of whether the name actually differs from the stored one.
    /// </summary>
    [Fact]
    public async Task Handle_FullNameNull_DoesNotCallIamContextFacade()
    {
        // GIVEN an existing profile for user 1
        var existing = new ProfileAggregate(1, ProfileRole.Producer, "Existing Name", "jane@example.com");
        _profileRepository.FindByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreateOrUpdateProfileCommand(
            UserId: 1,
            FullName: null,
            Email: null,
            Phone: null,
            JobTitle: null,
            Language: null,
            Location: null,
            SpecialtyArea: "Viticulture",
            PhotoUrl: null);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the facade is never invoked — no FullName was supplied on this partial update
        Assert.True(result.IsSuccess);
        await _iamContextFacade.DidNotReceive().UpdateFullNameAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    ///     GIVEN an existing profile
    ///     WHEN a <see cref="CreateOrUpdateProfileCommand"/> is handled with a whitespace-only
    ///     FullName
    ///     THEN <see cref="IIamContextFacade.UpdateFullNameAsync"/> is NOT called — the
    ///     production guard is <c>IsNullOrWhiteSpace</c>, not just a null check.
    /// </summary>
    [Fact]
    public async Task Handle_FullNameWhitespaceOnly_DoesNotCallIamContextFacade()
    {
        // GIVEN an existing profile for user 1
        var existing = new ProfileAggregate(1, ProfileRole.Producer, "Existing Name", "jane@example.com");
        _profileRepository.FindByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var command = new CreateOrUpdateProfileCommand(
            UserId: 1,
            FullName: "   ",
            Email: null,
            Phone: null,
            JobTitle: null,
            Language: null,
            Location: null,
            SpecialtyArea: null,
            PhotoUrl: null);

        // WHEN the command is handled
        var result = await _sut.Handle(command, CancellationToken.None);

        // THEN the facade is never invoked — whitespace-only name is guarded out
        Assert.True(result.IsSuccess);
        await _iamContextFacade.DidNotReceive().UpdateFullNameAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
