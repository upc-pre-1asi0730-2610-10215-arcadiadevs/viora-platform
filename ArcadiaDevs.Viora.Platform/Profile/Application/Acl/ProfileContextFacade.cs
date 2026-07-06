using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.Acl;

/// <summary>
///     ACL facade implementation that delegates to <see cref="IProfileRepository" />
///     for cross-boundary profile provisioning.
/// </summary>
/// <remarks>
///     Idempotent: <see cref="EnsureProfile" /> is a no-op if a profile already
///     exists for the given userId. The profile is created with the given
///     <c>role</c> parameter, defaulting to <c>Role=Producer</c> to match
///     OS's <c>ProfileCommandServiceImpl.ensureProfile</c> semantics.
/// </remarks>
public class ProfileContextFacade(
    IProfileRepository profileRepository,
    IUnitOfWork unitOfWork) : IProfileContextFacade
{
    /// <inheritdoc />
    public async Task EnsureProfile(
        int userId,
        string fullName,
        string email,
        string? phone = null,
        ProfileRole role = ProfileRole.Producer,
        CancellationToken ct = default)
    {
        var existing = await profileRepository.FindByUserIdAsync(userId, ct);
        if (existing is not null)
            return;

        var profile = new ProfileAggregate(
            userId,
            role,
            fullName,
            email,
            phone);

        await profileRepository.AddAsync(profile, ct);
        await unitOfWork.CompleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task<ProfileSummary?> GetProfileSummaryAsync(int userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.FindByUserIdAsync(userId, ct);
        return profile is null
            ? null
            : new ProfileSummary(profile.FullName, profile.Email, profile.Phone, profile.Role);
    }

    /// <inheritdoc />
    public async Task DeleteByUserIdAsync(int userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.FindByUserIdAsync(userId, ct);
        if (profile is null)
            return;

        profileRepository.Remove(profile);
        await unitOfWork.CompleteAsync(ct);
    }

    /// <inheritdoc />
    public async Task<string?> GetDisplayNameAsync(int userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.FindByUserIdAsync(userId, ct);
        return profile?.FullName;
    }

    /// <inheritdoc />
    public async Task<string?> GetPhotoUrlAsync(int userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.FindByUserIdAsync(userId, ct);
        return profile?.PhotoUrl;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SpecialistProfileSummary>> FindSpecialistProfilesAsync(CancellationToken ct = default)
    {
        var profiles = await profileRepository.FindByRoleAsync(ProfileRole.Specialist, ct);
        return profiles.Select(ToSpecialistProfileSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<SpecialistProfileSummary?> GetSpecialistProfileAsync(int userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.FindByUserIdAsync(userId, ct);
        return profile is null || profile.Role != ProfileRole.Specialist
            ? null
            : ToSpecialistProfileSummary(profile);
    }

    /// <inheritdoc />
    public async Task SetProBadgeAsync(int userId, bool enabled, CancellationToken ct = default)
    {
        var profile = await profileRepository.FindByUserIdAsync(userId, ct);
        if (profile is null)
            return;

        profile.ApplyUpdate(showProBadge: enabled);
        profileRepository.Update(profile);
        await unitOfWork.CompleteAsync(ct);
    }

    private static SpecialistProfileSummary ToSpecialistProfileSummary(ProfileAggregate profile)
    {
        return new SpecialistProfileSummary(
            profile.UserId,
            profile.FullName,
            profile.PhotoUrl,
            profile.Latitude,
            profile.Longitude,
            profile.ServiceRadiusKm,
            profile.ServiceTags,
            profile.Availability,
            profile.ShowProBadge);
    }
}
