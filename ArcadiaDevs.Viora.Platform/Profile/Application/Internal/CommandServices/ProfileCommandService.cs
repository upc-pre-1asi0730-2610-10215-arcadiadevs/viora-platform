using ArcadiaDevs.Viora.Platform.Iam.Interfaces.Acl;
using ArcadiaDevs.Viora.Platform.Profile.Application.CommandServices;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Commands;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Profile.Domain.Repositories;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Repositories;
using ProfileAggregate = ArcadiaDevs.Viora.Platform.Profile.Domain.Model.Aggregates.Profile;

namespace ArcadiaDevs.Viora.Platform.Profile.Application.Internal.CommandServices;

/// <summary>
///     Handles profile create-or-update commands with upsert semantics.
/// </summary>
public class ProfileCommandService(
    IProfileRepository profileRepository,
    IIamContextFacade iamContextFacade,
    IUnitOfWork unitOfWork) : IProfileCommandService
{
    /// <inheritdoc />
    public async Task<Result<(ProfileAggregate Profile, bool Created), Error>> Handle(
        CreateOrUpdateProfileCommand command,
        CancellationToken ct)
    {
        var existing = await profileRepository.FindByUserIdAsync(command.UserId, ct);

        ProfileAggregate profile;
        bool created;

        if (existing is null)
        {
            // Create path — default role to Producer per spec REQ.
            profile = new ProfileAggregate(
                command.UserId,
                ProfileRole.Producer,
                command.FullName ?? string.Empty,
                command.Email ?? string.Empty,
                command.Phone,
                command.JobTitle,
                command.Language,
                command.Location,
                command.SpecialtyArea,
                command.PhotoUrl,
                command.Latitude,
                command.Longitude,
                command.ServiceRadiusKm,
                command.ServiceTags,
                command.Availability,
                command.ShowProBadge ?? false);

            await profileRepository.AddAsync(profile, ct);
            created = true;
        }
        else
        {
            // Update path — null-safe partial update, Role untouched.
            existing.ApplyUpdate(
                command.FullName,
                command.Email,
                command.Phone,
                command.JobTitle,
                command.Language,
                command.Location,
                command.SpecialtyArea,
                command.PhotoUrl,
                command.Latitude,
                command.Longitude,
                command.ServiceRadiusKm,
                command.ServiceTags,
                command.Availability,
                command.ShowProBadge);

            profileRepository.Update(existing);
            profile = existing;
            created = false;
        }

        await unitOfWork.CompleteAsync(ct);

        // Keep the account's display name in sync with the profile so the
        // header and the account identity reflect the change immediately
        // (matches OS's ProfileCommandServiceImpl.handle).
        if (!string.IsNullOrWhiteSpace(command.FullName))
            await iamContextFacade.UpdateFullNameAsync(command.UserId, command.FullName.Trim(), ct);

        return new Result<(ProfileAggregate Profile, bool Created), Error>.Success((profile, created));
    }
}
