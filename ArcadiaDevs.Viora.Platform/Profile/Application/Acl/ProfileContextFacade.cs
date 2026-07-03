using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Profile.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.Acl;

/// <summary>
///     ACL facade implementation that delegates to <see cref="IProfileRepository" />
///     for cross-boundary profile provisioning.
/// </summary>
/// <remarks>
///     Idempotent: <see cref="EnsureProfile" /> is a no-op if a profile already
///     exists for the given userId. The profile is created with
///     <c>Role=Producer</c> by default, matching OS's
///     <c>ProfileCommandServiceImpl.ensureProfile</c> semantics.
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
        CancellationToken ct = default)
    {
        var existing = await profileRepository.FindByUserIdAsync(userId, ct);
        if (existing is not null)
            return;

        var profile = new Profile(
            userId,
            ProfileRole.Producer,
            fullName,
            email,
            phone);

        await profileRepository.AddAsync(profile, ct);
        await unitOfWork.CompleteAsync(ct);
    }
}
